(function () {
    'use strict';

    var chartButtons = document.querySelectorAll('[data-chart-period]');
    var monthlyChart = document.getElementById('homeChartMonthly');
    var yearlyChart = document.getElementById('homeChartYearly');

    chartButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            var yearly = button.dataset.chartPeriod === 'yearly';
            if (monthlyChart) monthlyChart.hidden = yearly;
            if (yearlyChart) yearlyChart.hidden = !yearly;
            chartButtons.forEach(function (item) {
                var active = item === button;
                item.classList.toggle('is-active', active);
                item.setAttribute('aria-pressed', active ? 'true' : 'false');
            });
        });
    });

    var balance = document.getElementById('homeBalanceValue');
    var capitalPart = document.getElementById('homeCapitalPart');
    var interestPart = document.getElementById('homeInterestPart');
    var balanceLabel = document.getElementById('homeBalanceLabel');
    var balanceButtons = document.querySelectorAll('[data-balance-type]');
    function updateBalance(type) {
        if (!balance) return;
        var capital = Number(balance.dataset.capital || 0);
        var interest = Number(balance.dataset.interest || 0);
        var total = capital + interest;
        var capitalSelected = type === 'capital';
        balance.textContent = capitalSelected ? balance.dataset.capitalDisplay : balance.dataset.interestDisplay;
        if (balanceLabel) balanceLabel.textContent = capitalSelected ? 'Capital pendiente' : 'Interés exigible';
        if (capitalPart) capitalPart.style.flexBasis = total > 0 ? (capital / total * 100) + '%' : '0%';
        if (interestPart) interestPart.style.flexBasis = total > 0 ? (interest / total * 100) + '%' : '0%';
        balanceButtons.forEach(function (button) {
            var active = button.dataset.balanceType === type;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
    }

    balanceButtons.forEach(function (button) {
        button.addEventListener('click', function () { updateBalance(button.dataset.balanceType); });
    });
    updateBalance('capital');

    var search = document.getElementById('dashboardQuickSearch');
    if (search) {
        search.addEventListener('input', function () {
            var value = search.value.trim().toLocaleLowerCase('es-PA');
            document.querySelectorAll('[data-dashboard-search-row]').forEach(function (row) {
                row.hidden = value !== '' && !row.textContent.toLocaleLowerCase('es-PA').includes(value);
            });
        });
        document.addEventListener('keydown', function (event) {
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
                event.preventDefault();
                search.focus();
            }
        });
    }
}());
