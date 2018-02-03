# CloudBuild

CloudBuild is a build platform for C# in the cloud (hosted on Microsoft Azure):
- Upload source files as blobs to Azure Storage Account (into 'input' container)
- REST API for requesting build, get build statuses/build error log
- Download executable from storage account (from 'output' container). For simplicity of this POC, output artifacts have public read access. 
- UI (based on Angular) is also provided for all operations: trigerring build, monitor build list/statuses, download artifacts, remove history - [UI screenshot](./screenshot1.JPG), [download screenshot](./download.JPG)

# Key facts
- Project is based on Azure Service Fabric/Micro services architecture. It has one service which is the web endpoint, and one which is the build cluster. The web endpoint communicates and triggers the build requests to the build cluster.
- We are building C# sources. Build is based on open source project Roslyn (a C# based compiler).
- A build is allocated to one of the clusters in the service. Build request is loading the source into memory, compiles it in memory and writes the resulting binary to the output container. Build process is isolated in the build service, within the worker thread that got the build request. For larger projects it would be recommended to trigger full build with storage/memory/kernel isolation.
- Scalability - both web cluster and build cluster can scale out using Service Fabric's scaling model
- Reliability - Service Fabric maintains reliabiliy, monitors the instances and retains their operability.
- Build request is asynchronous - control returns to client immediately. Client can poll build status.
- Error handling - any error (could be storage access error, compilation error) is reflected to user per build request.

# Test
- Full end to end test is included that does the following:
  - Upload source code
  - Trigger build in the cloud for our source code
  - Wait for it to finish and pull status. Assert successful compilation
  - Pull the binary, run it and validate expected result
- For completeness, more tests should be added: basic unit tests for the core logic, more e2e tests for rainy scenarios and failures, concurrency/load testing.

# Build & Installation
- Connection string for storage account is required (not checked in)
- Project is deployed on Azure over a Service Fabric cluster. It can be deployed on a 'party cluster' which stays active for an hour


  

