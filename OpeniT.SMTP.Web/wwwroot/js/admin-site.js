(function () {
    window.AdminSiteFunctions = {
        init: function () {
            //var prevScrollpos = 0;
            //$(window).scroll(function () {
            //    var currentScrollPos = $(this).scrollTop();;
            //    if (prevScrollpos > currentScrollPos) {
            //        $(".main-header").css("top", "0");
            //        $(".content-header.position-sticky").css("top", "calc(3.5rem + 1px)");
            //        $(".control-sidebar").css("top", "calc(3.5rem + 1px)");
            //    } else {
            //        if (currentScrollPos > 57) {
            //            $(".main-header").css("top", "calc(-3.5rem - 1px)");
            //            $(".content-header.position-sticky").css("top", "0");
            //            $(".control-sidebar").css("top", "0");
            //        }
            //    }

            //    prevScrollpos = currentScrollPos;
            //});

            var navSrollable = $('.sidebar nav.nav-scrollable');
            var activeNavLink = navSrollable.find('.nav-link.active');

            if (navSrollable.offset() && activeNavLink.offset()) {
                $(navSrollable).animate({
                    scrollTop: activeNavLink.offset().top - navSrollable.offset().top
                });
			}
        },
        initFileUploadContainer: function (fileUploadContainerElement) {
            $(window).bind("beforeunload", function (e) {
                if ($(fileUploadContainerElement).hasClass("upload-container-is-uploading")) {
                    return "Some upload is still in progress. Are you sure, you want to close?";
                }
            });
        }
    };
})();