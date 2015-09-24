$(function () {
    positionSlider();
    $(window).on('resize', positionSlider);
});

function positionSlider() {
    if ($('body').width() < 1183) {
        var firstP = $('.device-item-content > p:first-child');
        if (firstP.length > 0)
            $('.main-info').insertAfter(firstP);
        else
            $('.main-info').insertBefore($('.device-item-content'));
    }
    else {
        $('#main-row').prepend($('.main-info'));
    }
}