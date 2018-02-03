var app = angular.module('CloudBuildApp', ['ui.bootstrap']);
app.run(function () { });

app.controller('CloudBuildAppController', ['$rootScope', '$scope', '$http', '$timeout', function ($rootScope, $scope, $http, $timeout) {

    $scope.refresh = function () {
        $http.get('api/Builds?c=' + new Date().getTime())
            .then(function (data, status) {
                $scope.builds = data;
            }, function (data, status) {
                $scope.builds = undefined;
            });
    };

    $scope.remove = function (item) {
        $http.delete('api/Builds/' + item)
            .then(function (data, status) {
                $scope.refresh();
            })
    };

    $scope.add = function (item) {
        var fd = new FormData();
        fd.append('item', item);
        $http.put('api/Builds/' + item, fd, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (data, status) {
                $scope.refresh();
                $scope.item = undefined;
            })
    };
}]);