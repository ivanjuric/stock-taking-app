// Modal and Lightbox Keyboard Navigation
(function() {
    'use strict';
    
    document.addEventListener('keydown', function(e) {
        // Get current modal/lightbox from hash
        const hash = window.location.hash;
        if (!hash || hash === '#!' || hash === '#') return;
        
        let target;
        try {
            target = document.querySelector(hash);
        } catch (err) {
            return; // Invalid selector
        }
        if (!target) return;
        
        const isLightbox = target.classList.contains('gallery-lightbox');
        const isModal = target.classList.contains('modal') || target.classList.contains('confirm-modal');
        
        if (!isLightbox && !isModal) return;
        
        switch(e.key) {
            case 'Escape':
                e.preventDefault();
                closeModal();
                break;
                
            case 'ArrowLeft':
                if (isLightbox) {
                    e.preventDefault();
                    navigateLightbox(target, 'prev');
                }
                break;
                
            case 'ArrowRight':
                if (isLightbox) {
                    e.preventDefault();
                    navigateLightbox(target, 'next');
                }
                break;
        }
    });
    
    function closeModal() {
        // Use location.href to actually trigger :target update
        // Remove hash by navigating to same page without hash
        const url = window.location.pathname + window.location.search;
        window.location.replace(url + '#!');
        // Immediately clean up the #! from URL
        setTimeout(function() {
            history.replaceState(null, '', url);
        }, 10);
    }
    
    function navigateLightbox(current, direction) {
        const nav = current.querySelector('.gallery-nav');
        if (!nav) return;
        
        // Get only actual anchor links (not empty spans used as placeholders)
        const links = Array.from(nav.querySelectorAll('a[href^="#"]')).filter(
            link => link.getAttribute('href') && link.getAttribute('href') !== '#' && link.getAttribute('href') !== '#!'
        );
        
        if (links.length === 0) return;
        
        // Check link titles to determine which is prev and which is next
        let targetLink = null;
        
        for (const link of links) {
            const title = (link.getAttribute('title') || '').toLowerCase();
            if (direction === 'prev' && title === 'previous') {
                targetLink = link;
                break;
            } else if (direction === 'next' && title === 'next') {
                targetLink = link;
                break;
            }
        }
        
        if (targetLink) {
            targetLink.click();
        }
    }
})();
