document.getElementById('loadBtn').addEventListener('click', async () => {
    try {
        // Запрос к Minimal API эндпоинту
        const response = await fetch('/weatherforecast');
        const data = await response.json();

        const list = document.getElementById('weatherList');
        list.innerHTML = '';

        data.forEach(item => {
            const li = document.createElement('li');
            li.textContent = `${item.date}: ${item.temperatureC}°C / ${item.temperatureF}°F — ${item.summary}`;
            list.appendChild(li);
        });
    } catch (error) {
        console.error('Ошибка при получении данных:', error);
    }
}); 