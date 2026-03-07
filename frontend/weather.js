    async function fetchWeatherData() {
        try {
            const response = await fetch('http://localhost:5033/weatherforecast');
            const data = await response.json();
            updateWeatherTable(data);
        } catch (error) {
            console.error('Error fetching weather data:', error);
        }
    }
    fetchWeatherData();

    function updateWeatherTable(data) {
        const table = document.querySelector('table');
        const tbody = table.querySelector('tbody');
        tbody.innerHTML = ''; // Clear existing rows
        
        data.forEach(cityWeather => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${cityWeather.date}</td>
                <td>${cityWeather.temperatureC}°C</td>
                <td>${cityWeather.temperatureF}°F</td>
                <td>${cityWeather.summary}</td>
            `;
            tbody.appendChild(row);
        });
    }
