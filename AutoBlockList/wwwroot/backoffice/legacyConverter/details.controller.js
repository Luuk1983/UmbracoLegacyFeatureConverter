(function () {
    'use strict';

    angular.module('umbraco')
        .controller('legacyConverter.details.controller', LegacyConverterDetailsController);

    LegacyConverterDetailsController.$inject = ['$scope', '$http', '$location', '$routeParams', 'notificationsService'];

    function LegacyConverterDetailsController($scope, $http, $location, $routeParams, notificationsService) {
        var vm = this;

        // State
        vm.loading = false;
        vm.details = null;
        vm.filteredLogs = [];
        vm.filters = {
            showInfo: true,
            showWarning: true,
            showError: true
        };

        // Methods
        vm.backToHistory = backToHistory;
        vm.filterLogs = filterLogs;
        vm.formatDate = formatDate;
        vm.formatTime = formatTime;
        vm.getStatusIcon = getStatusIcon;
        vm.getStatusClass = getStatusClass;
        vm.getLogIcon = getLogIcon;
        vm.getLogClass = getLogClass;

        // Initialization
        init();

        function init() {
            var conversionId = $routeParams.id;
            if (!conversionId) {
                notificationsService.error('Error', 'No conversion ID provided');
                backToHistory();
                return;
            }

            loadDetails(conversionId);
        }

        function loadDetails(conversionId) {
            vm.loading = true;
            
            $http.get('/umbraco/backoffice/api/ConverterApi/GetConversionDetails', {
                params: { id: conversionId }
            })
                .then(function (response) {
                    vm.details = response.data;
                    vm.filteredLogs = vm.details.logs;
                    vm.loading = false;
                    filterLogs();
                })
                .catch(function (error) {
                    console.error('Error loading conversion details:', error);
                    notificationsService.error('Error', 'Failed to load conversion details');
                    vm.loading = false;
                });
        }

        function filterLogs() {
            if (!vm.details || !vm.details.logs) return;

            vm.filteredLogs = vm.details.logs.filter(function (log) {
                var level = log.level.toLowerCase();
                
                if (level === 'information' && !vm.filters.showInfo) return false;
                if (level === 'warning' && !vm.filters.showWarning) return false;
                if (level === 'error' && !vm.filters.showError) return false;
                
                return true;
            });
        }

        function backToHistory() {
            $location.path('/settings/legacyConverter/history');
        }

        function formatDate(dateString) {
            if (!dateString) return '';
            var date = new Date(dateString);
            return date.toLocaleString();
        }

        function formatTime(dateString) {
            if (!dateString) return '';
            var date = new Date(dateString);
            return date.toLocaleTimeString();
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

        function getLogIcon(level) {
            switch (level.toLowerCase()) {
                case 'information':
                    return 'icon-info';
                case 'warning':
                    return 'icon-alert';
                case 'error':
                    return 'icon-delete';
                default:
                    return 'icon-help';
            }
        }

        function getLogClass(level) {
            switch (level.toLowerCase()) {
                case 'information':
                    return 'color-blue';
                case 'warning':
                    return 'color-orange';
                case 'error':
                    return 'color-red';
                default:
                    return '';
            }
        }
    }
})();
