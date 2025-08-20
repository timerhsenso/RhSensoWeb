function getFormattedDateTime() {
    const date = new Date();

    const optionsDate = {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    };
    const formattedDate = date.toLocaleDateString('pt-BR', optionsDate);

    const optionsTime = {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false
    };
    const formattedTime = date.toLocaleTimeString('pt-BR', optionsTime);

    return `Hoje é ${formattedDate}. São ${formattedTime}`;
}

function startClock(elementId) {
    const clockElement = document.getElementById(elementId);
    if (!clockElement) {
        console.error(`Elemento com ID '${elementId}' não encontrado.`);
        return;
    }

    function updateClock() {
        clockElement.textContent = getFormattedDateTime();
    }

    // Atualiza o relógio a cada segundo
    setInterval(updateClock, 1000);

    // Atualiza imediatamente ao iniciar
    updateClock();
}


document.addEventListener("DOMContentLoaded", function () {
    startClock("meuRelogio");
});
