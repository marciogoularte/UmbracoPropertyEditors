angular.module('umbraco')
.controller('Codevos.PartialSorterController', function ($scope, $http, $routeParams, assetsService) {
    var language = '';

    $scope.loading = true;
    $scope.error = false;
    $scope.docTypes = [];

    $scope.sortableOptions = {
        axis: 'y'
    };

    $scope.translate = function (item) {
        return item.translations ? item.translations[language] : item.alias;
    };

    var sortPartials = function (docTypes) {
        for (var i = 0; i < docTypes.length; i++) {
            var docType = docTypes[i];
            var docTypeAlias = docType.alias;
            var partialNames = $scope.model.value[docTypeAlias];

            if (partialNames != null && partialNames.length) {
                var partials = docType.partials;
                docType.partials = [];

                for (var j = 0; j < partialNames.length; j++) {
                    var partialName = partialNames[j];

                    var k = 0;
                    while (k < partials.length) {
                        if (partials[k].alias === partialName) {
                            docType.partials.push(partials[k]);
                            partials.splice(k, 1);
                            k = partials.length;
                        }
                        k++;
                    }
                }

                for (var j = 0; j < partials.length; j++) {
                    docType.partials.push(partials[j]);
                }
            }
        }

        return docTypes;
    };

    var addWatch = function () {
        $scope.$watch('docTypes', function (newValue, oldValue) {
            var modelValue = {};

            for (var i = 0; i < newValue.length; i++) {
                var docType = newValue[i];
                var partialNames = [];

                for (var j = 0; j < docType.partials.length; j++) {
                    partialNames.push(docType.partials[j].alias);
                }

                modelValue[docType.alias] = partialNames;
            }

            $scope.model.value = modelValue;
        }, true);
    };

    var dataLoaded = function (docTypes) {
        if ($scope.model.value && $scope.model.value != '') {
            docTypes = sortPartials(docTypes);
        }
        $scope.docTypes = docTypes;
        addWatch();
    };

    var location = window.location.toString()

    $http.get(location.substring(0, location.indexOf('#')) + '/backoffice/api/partialsorterconfig/get', { params: { pageId: $routeParams.id, propertyAlias: $scope.model.alias } })
    .success(function (data) {
        language = data.language;
        dataLoaded(data.docTypes);
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = true;
        $scope.loading = false;
    });

    assetsService.loadCss($scope.model.view.substring(0, $scope.model.view.lastIndexOf('/') + 1) + 'codevos.partialsorter.css');
});