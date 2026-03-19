(function () {
    'use strict';

    angular.module('umbraco')
        .controller('legacyConverter.overview.controller', LegacyConverterOverviewController);

    LegacyConverterOverviewController.$inject = ['$scope', '$http', '$location', 'notificationsService'];

    function LegacyConverterOverviewController($scope, $http, $location, notificationsService) {
        var vm = this;

        // State
        vm.loadingConverters = false;
        vm.loadingDocumentTypes = false;
        vm.converting = false;
        vm.converters = [];
        vm.selectedConverter = null;
        vm.documentTypes = [];
        vm.selectedDocumentTypes = [];
        vm.allDocTypesSelected = false;
        vm.isTestRun = false;
        vm.conversionResult = null;

        // Methods
        vm.selectConverter = selectConverter;
        vm.toggleDocumentType = toggleDocumentType;
        vm.toggleSelectAllDocTypes = toggleSelectAllDocTypes;
        vm.startConversion = startConversion;
        vm.viewHistory = viewHistory;
        vm.viewConversionDetails = viewConversionDetails;
        vm.formatDuration = formatDuration;

        // Initialization
        init();

        function init() {
            loadConverters();
        }

        function loadConverters() {
            vm.loadingConverters = true;
            
            $http.get('/umbraco/backoffice/api/ConverterApi/GetConverters')
                .then(function (response) {
                    vm.converters = response.data;
                    vm.loadingConverters = false;
                    
                    // Auto-select first converter if only one available
                    if (vm.converters.length === 1) {
                        selectConverter(vm.converters[0]);
                    }
                })
                .catch(function (error) {
                    console.error('Error loading converters:', error);
                    notificationsService.error('Error', 'Failed to load converters');
                    vm.loadingConverters = false;
                });
        }

        function selectConverter(converter) {
            vm.selectedConverter = converter;
            vm.documentTypes = [];
            vm.selectedDocumentTypes = [];
            vm.conversionResult = null;
            
            loadDocumentTypes();
        }

        function loadDocumentTypes() {
            if (!vm.selectedConverter) return;

            vm.loadingDocumentTypes = true;
            
            $http.get('/umbraco/backoffice/api/ConverterApi/GetDocumentTypes', {
                params: { converterName: vm.selectedConverter.name }
            })
                .then(function (response) {
                    vm.documentTypes = response.data.map(function (dt) {
                        dt.selected = false;
                        return dt;
                    });
                    vm.loadingDocumentTypes = false;
                })
                .catch(function (error) {
                    console.error('Error loading document types:', error);
                    notificationsService.error('Error', 'Failed to load document types');
                    vm.loadingDocumentTypes = false;
                });
        }

        function toggleDocumentType(docType) {
            docType.selected = !docType.selected;
            updateSelectedDocumentTypes();
        }

        function toggleSelectAllDocTypes() {
            vm.allDocTypesSelected = !vm.allDocTypesSelected;
            
            vm.documentTypes.forEach(function (dt) {
                dt.selected = vm.allDocTypesSelected;
            });
            
            updateSelectedDocumentTypes();
        }

        function updateSelectedDocumentTypes() {
            vm.selectedDocumentTypes = vm.documentTypes
                .filter(function (dt) { return dt.selected; })
                .map(function (dt) { return dt.key; });
            
            vm.allDocTypesSelected = vm.selectedDocumentTypes.length === vm.documentTypes.length;
        }

        function startConversion() {
            if (!vm.selectedConverter) {
                notificationsService.warning('Warning', 'Please select a converter first');
                return;
            }

            // If no document types selected, convert all
            var documentTypeKeys = vm.selectedDocumentTypes.length > 0 
                ? vm.selectedDocumentTypes 
                : null;

            var confirmMessage = vm.isTestRun
                ? 'Start test run for ' + vm.selectedConverter.name + '?'
                : 'Start conversion for ' + vm.selectedConverter.name + '? This will modify your data.';

            if (!confirm(confirmMessage)) {
                return;
            }

            vm.converting = true;
            vm.conversionResult = null;

            var request = {
                converterType: vm.selectedConverter.name,
                selectedDocumentTypeKeys: documentTypeKeys,
                isTestRun: vm.isTestRun,
                performingUserKey: null // Will be set by server
            };

            $http.post('/umbraco/backoffice/api/ConverterApi/ExecuteConversion', request)
                .then(function (response) {
                    vm.conversionResult = response.data;
                    vm.converting = false;

                    var statusMessage = vm.isTestRun ? 'Test run completed' : 'Conversion completed';
                    
                    if (vm.conversionResult.status === 'Completed') {
                        notificationsService.success('Success', statusMessage + ' successfully!');
                    } else if (vm.conversionResult.status === 'CompletedWithErrors') {
                        notificationsService.warning('Warning', statusMessage + ' with some errors');
                    } else {
                        notificationsService.error('Error', statusMessage + ' with failures');
                    }

                    // Reload document types to show updated counts
                    loadDocumentTypes();
                })
                .catch(function (error) {
                    console.error('Error executing conversion:', error);
                    notificationsService.error('Error', 'Conversion failed: ' + (error.data?.error || error.message));
                    vm.converting = false;
                });
        }

        function viewHistory() {
            $location.path('/settings/legacyConverter/history');
        }

        function viewConversionDetails(conversionId) {
            $location.path('/settings/legacyConverter/details/' + conversionId);
        }

        function formatDuration(duration) {
            if (!duration) return '';
            
            var parts = duration.split(':');
            if (parts.length < 3) return duration;
            
            var hours = parseInt(parts[0]);
            var minutes = parseInt(parts[1]);
            var seconds = parseInt(parts[2].split('.')[0]);
            
            var result = [];
            if (hours > 0) result.push(hours + 'h');
            if (minutes > 0) result.push(minutes + 'm');
            if (seconds > 0 || result.length === 0) result.push(seconds + 's');
            
            return result.join(' ');
        }
    }
})();
