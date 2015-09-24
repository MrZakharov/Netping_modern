var floatingMenuVisible = false;

$(function () {
    var body = $("html, body");

    showHideMenu();

    $(window).on('scroll', showHideMenu);

    $('.device-item-menu a').on('click', function (event) {
        event.preventDefault();
        var anchor = $(this).attr('href');
        console.log($(anchor));
        body.stop().animate({ scrollTop: $(anchor).offset().top - $('.floating-menu').height() - 20 });
    })

})

function showHideMenu() {
    if ($(window).scrollTop() >= 500 && !floatingMenuVisible) {
        floatingMenuVisible = true;
        $('.floating-menu').fadeIn();
    }
    else if (floatingMenuVisible && $(window).scrollTop() < 500) {
        floatingMenuVisible = false;
        $('.floating-menu').fadeOut();
    }
}