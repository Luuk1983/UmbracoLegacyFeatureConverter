(function () {
    'use strict';

    angular.module('umbraco')
        .controller('legacyConverter.history.controller', LegacyConverterHistoryController);

    LegacyConverterHistoryController.$inject = ['$scope', '$http', '$location', 'notificationsService'];

    function LegacyConverterHistoryController($scope, $http, $location, notificationsService) {
        var vm = this;

        // State
        vm.loading = false;
        vm.history = {
            items: [],
            pageNumber: 1,
            pageSize: 20,
            totalPages: 0,
            totalItems: 0
        };

        // Methods
        vm.viewDetails = viewDetails;
        vm.backToOverview = backToOverview;
        vm.nextPage = nextPage;
        vm.prevPage = prevPage;
        vm.goToPage = goToPage;
        vm.formatDate = formatDate;
        vm.getStatusIcon = getStatusIcon;
        vm.getStatusClass = getStatusClass;

        // Initialization
        init();

        function init() {
            loadHistory(1);
        }

        function loadHistory(page) {
            vm.loading = true;
            
            $http.get('/umbraco/backoffice/api/ConverterApi/GetHistory', {
                params: { page: page, pageSize: vm.history.pageSize }
            })
                .then(function (response) {
                    vm.history = response.data;
                    vm.loading = false;
                })
                .catch(function (error) {
                    console.error('Error loading history:', error);
                    notificationsService.error('Error', 'Failed to load conversion history');
                    vm.loading = false;
                });
        }

        function viewDetails(conversionId) {
            $location.path('/settings/legacyConverter/details/' + conversionId);
        }

        function backToOverview() {
            $location.path('/settings/legacyConverter/overview');
        }

        function nextPage() {
            if (vm.history.pageNumber < vm.history.totalPages) {
                loadHistory(vm.history.pageNumber + 1);
            }
        }

        function prevPage() {
            if (vm.history.pageNumber > 1) {
                loadHistory(vm.history.pageNumber - 1);
            }
        }

        function goToPage(pageNumber) {
            loadHistory(pageNumber);
        }

        function formatDate(dateString) {
            if (!dateString) return '';
            var date = new Date(dateString);
            return date.toLocaleString();
        }

        function getStatusIcon(status) {
            switch (status) {
                case 'Completed':
                    return 'icon-check';
                case 'CompletedWithErrors':
                    return 'icon-info';
                case 'Failed':
                    return 'icon-delete';
                case 'Running':
                    return 'icon-loading';
                default:
                    return 'icon-help';
            }
        }

        function getStatusClass(status) {
            switch (status) {
                case 'Completed':
                    return 'color-green';
                case 'CompletedWithErrors':
                    return 'color-orange';
                case 'Failed':
                    return 'color-red';
                default:
                    return '';
            }
        }
    }
})();
