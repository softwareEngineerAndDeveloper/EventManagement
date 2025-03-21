/*!
 * Etkinlik Yönetim - Admin Dashboard
 */

(function($) {
    "use strict";

    // Dark Mode Toggle Ayarları
    function initDarkMode() {
        // Kullanıcı tercihini localStorage'dan al
        const currentTheme = localStorage.getItem('theme') || 'light';
        
        // Tema tercihine göre data-theme özelliğini ayarla
        document.documentElement.setAttribute('data-theme', currentTheme);
        
        // Toggle butonunun durumunu ayarla
        const darkModeToggle = document.getElementById('darkModeToggle');
        if (darkModeToggle) {
            darkModeToggle.checked = currentTheme === 'dark';
        }
    }

    // Dark Mode Toggle işlevi
    function toggleDarkMode(e) {
        if (e.target.checked) {
            document.documentElement.setAttribute('data-theme', 'dark');
            localStorage.setItem('theme', 'dark');
        } else {
            document.documentElement.setAttribute('data-theme', 'light');
            localStorage.setItem('theme', 'light');
        }
    }

    // Sayfa yüklendiğinde Dark Mode ayarlarını başlat
    $(document).ready(function() {
        initDarkMode();
        
        // Toggle butonuna olay dinleyicisi ekle
        $('#darkModeToggle').on('change', toggleDarkMode);
    });

    // Sidebar Toggler
    $("#sidebarToggle, #sidebarToggleTop").on('click', function(e) {
        $("body").toggleClass("sidebar-toggled");
        $(".sidebar").toggleClass("toggled");
        if ($(".sidebar").hasClass("toggled")) {
            $('.sidebar .collapse').collapse('hide');
        }
    });

    // Otomatik Sidebar Küçültme
    var windowWidth = $(window).width();
    if (windowWidth < 768) {
        $('.sidebar .collapse').collapse('hide');
    }
    
    // Ekran Boyutu Küçük Olduğunda Sidebar'ı Gizle
    $(window).resize(function() {
        windowWidth = $(window).width();
        if (windowWidth < 768) {
            $('.sidebar .collapse').collapse('hide');
        }
        
        // Ekran mobil boyutuna geldiğinde otomatik sidebar collapse
        if (windowWidth < 480 && !$(".sidebar").hasClass("toggled")) {
            $("body").addClass("sidebar-toggled");
            $(".sidebar").addClass("toggled");
            $('.sidebar .collapse').collapse('hide');
        }
    });

    // Scroll to top buton için
    $(document).on('scroll', function() {
        var scrollDistance = $(this).scrollTop();
        if (scrollDistance > 100) {
            $('.scroll-to-top').fadeIn();
        } else {
            $('.scroll-to-top').fadeOut();
        }
    });

    // Smooth scrolling
    $(document).on('click', 'a.scroll-to-top', function(e) {
        var $anchor = $(this);
        $('html, body').stop().animate({
            scrollTop: ($($anchor.attr('href')).offset().top)
        }, 500, 'easeInOutExpo');
        e.preventDefault();
    });

    // Kullanıcı menüsü dropdown
    $('.nav-item.dropdown').hover(
        function() {
            $(this).find('.dropdown-menu').stop(true, true).delay(200).fadeIn(300);
        },
        function() {
            $(this).find('.dropdown-menu').stop(true, true).delay(200).fadeOut(300);
        }
    );

    // Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // Bootstrap popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl)
    });

})(jQuery); 