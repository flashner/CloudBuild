namespace CloudBuildData.Controllers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using Utils;

    [Route("api/[controller]")]
    public class BuildDataController : Controller
    {
        private readonly IReliableStateManager stateManager;

        public BuildDataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // GET api/BuildData
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            CancellationToken ct = new CancellationToken();

            IReliableDictionary<string, string> buildsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("builds");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<string, string>> list = await buildsDictionary.CreateEnumerableAsync(tx);

                IAsyncEnumerator<KeyValuePair<string, string>> enumerator = list.GetAsyncEnumerator();

                List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

                while (await enumerator.MoveNextAsync(ct))
                {
                    result.Add(enumerator.Current);
                }

                return this.Json(result);
            }
        }

        private async Task UpdateBuildStatus(string name, string status)
        {
            IReliableDictionary<string, string> buildsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("builds");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                string timestampStatus = $"{DateTime.UtcNow}: {status}";
                await buildsDictionary.AddOrUpdateAsync(tx, name, timestampStatus, (key, oldvalue) => timestampStatus);
                await tx.CommitAsync();
            }
        }

        private async Task StartBuildAsync(string name)
        {
            // read source from blob storage, compile it and write image to output folder
            // todo: all validity checks below use catch Exception for simplicity of POC which 
            // should be converted to specific exception types
            string src;
            try
            {
                await UpdateBuildStatus(name, "Reading source blob...");
                src = BlobClient.ReadBlob($"{name}.cs");
            }
            catch (Exception)
            {
                await UpdateBuildStatus(name, "Failed reading source blob.");
                return;
            }

            byte[] compiled;
            try
            {
                await UpdateBuildStatus(name, "Starting build..");
                compiled = DotNetCompiler.CompileConsoleApp(src);
            }
            catch (Exception ex)
            {
                await UpdateBuildStatus(name, $"Build Failed: {ex}");
                return;
            }
            try
            {
                await UpdateBuildStatus(name, "Writing image..");

                // we use 'exe_' suffix to avoid browser warning on unsecure content
                BlobClient.WriteBlob("output", $"{name}.exe_", compiled);
            }
            catch (Exception)
            {
                await UpdateBuildStatus(name, $"Write image Failed!");
                return;
            }

            await UpdateBuildStatus(name, "Success!");
        }

        // PUT api/BuildData/name
        [HttpPut("{name}")]
        public async Task<string> Put(string name)
        {
            await UpdateBuildStatus(name, "Starting build async..");

            // we deliberately not using async so that build will start in the bg
            // and we could return immediately
            StartBuildAsync(name);

            return name;
        }



        // DELETE api/BuildData/name
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            IReliableDictionary<string, string> buildsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("builds");

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                if (await buildsDictionary.ContainsKeyAsync(tx, name))
                {
                    await buildsDictionary.TryRemoveAsync(tx, name);
                    await tx.CommitAsync();
                    return new OkResult();
                }
                else
                {
                    return new NotFoundResult();
                }
            }
        }
    }
}