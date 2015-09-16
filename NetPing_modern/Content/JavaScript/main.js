$(document).ready(function($){
	$('.down_list').hide();
	$('.nav_li').hover(function(){
			$(this).find('.down_list').stop(true).slideDown(200);
		},
		function(){
			$(this).find('.down_list').slideUp(200);	
		}
		);	
	//-------------------------------
	if(!Modernizr.input.placeholder){

	$('[placeholder]').focus(function() {
	  var input = $(this);
	  if (input.val() == input.attr('placeholder')) {
		input.val('');
		input.removeClass('placeholder');
	  }
	}).blur(function() {
	  var input = $(this);
	  if (input.val() == '' || input.val() == input.attr('placeholder')) {
		input.addClass('placeholder');
		input.val(input.attr('placeholder'));
	  }
	}).blur();
	$('[placeholder]').parents('form').submit(function() {
	  $(this).find('[placeholder]').each(function() {
		var input = $(this);
		if (input.val() == input.attr('placeholder')) {
		  input.val('');
		}
	  })
	});
	};


    var headers3 = $('.long-description h3');
    if (headers3.length > 0 && $.fn.sshutter)
    {
        headers3.sshutter();
    }
    headers3.wrapInner('<span class="dashed"></span>')
	$('.text *').attr('style', '');
}); 


var CatalogJs = (function () {
    function Catalog() {
    };

    Catalog.prototype.init = function () {
        $('input[name="compare"]:checkbox').change(function () {
            var enabledCompare = ($('input[name="compare"]:checkbox:checked').length > 1);
            var compareBtnSelector = '.comparebtn';
            if (enabledCompare) {
                $(compareBtnSelector).removeAttr('disabled');
                $(compareBtnSelector).removeClass('disabled');
            }
            else {
                $(compareBtnSelector).attr('disabled', 'disabled');
                $(compareBtnSelector).addClass('disabled');
            }
        });

		$('#productsPage').addClass('active');
    };

    return new Catalog();
})();