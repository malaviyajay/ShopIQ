document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const truck = document.querySelector('.cart-animation');

        // Start animation
        truck.style.transform = 'translateX(120vw)'; // moves across screen

        // Reset after animation
        setTimeout(() => {
            truck.style.transform = 'translateX(0)';
        }, 2200);
    });
});
