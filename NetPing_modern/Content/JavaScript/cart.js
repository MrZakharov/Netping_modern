$(document).ready(function () {
    updateCartCount();

    $("#shopItem").on("click", function () {
        addProduct('body');
    });

    $(".to-cart .counter").on("click", function (event) {
        event.preventDefault();
    });

    $(".to-cart .counter").keypress(function (event) {
        if (event.keyCode == 13) {
            event.preventDefault();
            event.stopPropagation();
        }
    });

    $(".to-cart .add").on("click", addOneItem);

    $(".to-cart .remove").on("click", removeOneItem);


    //$("#cartPopup").click(function (event) {
    //    event.preventDefault();
    //    event.stopPropagation();
    //});

    $("#clearCartButton").click(function () {
        clearCart();
    });

    $("#closeCartButton").click(function (event) {
        event.preventDefault();
        event.stopPropagation();
        hidePopup();
    });

    $("#continueCartButton").click(function () {
        $("#cartItems").hide();
        $("#cartAbout").show();
        $("#continueCartButton").hide();
        $("#closeCartButton").hide();
        $("#sendDataCartButton").show();
        $("#clearCartButton").hide();
        $("#backCartButton").show();
    })

    $("#backCartButton").click(function () {
        $("#continueCartButton").show();
        $("#sendDataCartButton").hide();
        $("#clearCartButton").show();
        $("#closeCartButton").show();
        $("#backCartButton").hide();
        $("#cartAbout").hide();
        $("#cartItems").show();
    })

    $("#sendDataCartButton").click(function () {


        var data = getData() || [];
        if (data.length == 0) return;
        var fio = $("#FIOK").val().trim();
        var email = $("#emailK").val().trim();
        var regex = /^([a-zA-Z0-9_.+-])+\@(([a-zA-Z0-9-])+\.)+([a-zA-Z0-9]{2,4})+$/;
        var address = $("#addressK").val().trim();
        var isvalid = true;
        if (!regex.test(email)) {
            isvalid = false;
            $("#emailK").addClass('invalid');
        }
        if (fio.length == 0) {
            isvalid = false;
            $("#FIOK").addClass('invalid');
        }
        if (address.length == 0) {
            isvalid = false;
            $("#addressK").addClass('invalid');
        }
        if (!isvalid)
            return;
        var requisites = $("#requisites").val();
        var phone = $("#phone").val().trim();
        //var shipping = $("input:radio[name=shipping]:checked").val();
        var requestData = {
            Name: fio,
            EMail: email,
            Address: address,
            Requisites: requisites,
            //Shipping: shipping,
            Data: data,
            Phone: phone
        };

        $('#cartPopup').append('<div id="cart-preloader" class="cart-preloader"></div>');

        $.ajax({
            type: "POST",
            url: "/cart/SendCartMail",
            data: requestData,
            success: function () {
                $('#cart-preloader').remove();
                clearCart();
                alert(Res.get('OrderSent'));
            },
            error: function () {
                $('#cart-preloader').remove();
                alert(Res.get('OrderErrorMessage'));
            },
            dataType: "json"
        });
    });

    $('.device-item-buttons input[type=text]').on('keydown', onlyNumeric);

    $('.device-item-buttons input[type=text]').on('blur change', countInput);
})

var C = jaaulde.utils.cookies;
//        C.del(true);
C.setOptions({ expiresAt: 1 });

var isCartPopupOpened = false;

function updateCartCount() {
    var data = getData() || [];
    var cartcount = 0;
    $('.in-cart.btn-primary').removeClass('in-cart').addClass('buy-button');
    $('.counter.in-cart').removeClass('in-cart').val(1);
    for (i = 0; i < data.length; i++) {
        cartcount += parseInt(data[i].count);
        $('.buy-button[data-device-id="' + data[i].ID + '"]').removeClass('buy-button').addClass('in-cart')
                                                        .siblings('.counter').addClass('in-cart').val(data[i].count);
    }
    $('.cart-count').text(cartcount);

    $(".header .cart, .btn-primary.in-cart").off("click");
    $('.cat_item .action .price span, .buy-button').off("click");
    $(".header .cart, .btn-primary.in-cart").on("click", function (event) {
        var container = $(this);
        showPopup(container);
    });

    $('.cat_item .action .price span, .buy-button').on("click", function (event) {
        var item = $(this).parents('.cat_item');
        addProduct(item);
    });
    $('#continueCartButton').removeAttr('disabled');
    if (data.length == 0)
        $('#continueCartButton').attr('disabled', 'disabled');

}

function addProduct(itemcontainer) {
    var ID = $(itemcontainer).find(".ID")[0].innerHTML.trim();
    var name = $(itemcontainer).find(".Name")[0].innerHTML.trim();
    var price = $(itemcontainer).find(".Price")[0].innerHTML.trim();
    var photoURL = $(itemcontainer).find(".PhotoURL")[0].innerHTML.trim();
    var key = $(itemcontainer).find(".Key")[0].innerHTML.trim();
    var data = getData() || [];
    var item = data.filter(function (element) {
        return element.ID == ID;
    });
    var count = parseInt($(itemcontainer).find(".counter").val());
    if (item.length > 0) {
        item[0].count += count;
        C.set(ID, JSON.stringify(item[0]));
    }
    else {
        C.set(ID, JSON.stringify({
            name: name,
            count: count,
            price: price,
            photoURL: photoURL,
            key: key,
            ID: ID
        }));
    }
    showPopup($(itemcontainer).find('div.btn.btn-primary.buy-button'));
    updateCartCount();
}

function clearCart() {
    $(".hiddenID").each(function () {
        C.del(this.innerHTML);
    });
    hidePopup();
}


function showPopup(container) {
    if (isCartPopupOpened)
        return;

    isCartPopupOpened = true;
    var cartPopup = $('#cartItems');
    cartPopup.empty();
    var data = getData() || [];
    var itemTemplateSH = $("#cartItemTemplate").clone().removeAttr('id');
    updateSum(data);

    for (var i = 0; i < data.length; i++) {
        var product = data[i];
        var itemTemplate = itemTemplateSH.clone();
        itemTemplate.find(".hiddenID")[0].innerHTML = product.ID;
        var itemImg = itemTemplate.find(".shopPopupItemImg")[0];
        itemImg.src = product.photoURL;
        var itemName = itemTemplate.find(".shopItemName")[0];
        itemName.innerHTML = '<a href="/products/' + product.key + '">' + product.name + '</a>';
        var itemPrice = itemTemplate.find(".shopItemPrice")[0];
        itemPrice.innerHTML = product.price;
        var itemCount = itemTemplate.find(".shopItemCount")[0];
        itemCount.value = product.count;
        var itemPriceSum = itemTemplate.find(".shopItemPriceSum")[0];
        itemPriceSum.innerHTML = parseInt(product.price) * parseInt(product.count);
        cartPopup.append(itemTemplate);
        var itemCountAdd = itemTemplate.find(".add-one")[0];
        $(itemCountAdd).data("device-id", product.ID);

        var itemCountRemove = itemTemplate.find(".remove-one")[0];
        $(itemCountRemove).data("device-id", product.ID);
    }

    $(".cart-item-counter-control.add-one").on('click', addOneItem);
    $(".cart-item-counter-control.remove-one").on('click', removeOneItem);
    $(".cart-item-counter-control").on("click", function (event) {
        event.preventDefault();
        value = parseInt($(this).siblings('.counter').val());
        if (!value || parseInt(value) <= 0) value = 1;
        var productContainer = $(this).parents('.shopPopupItem');
        productContainer.find(".shopItemPriceSum")[0].innerHTML = parseInt(value) *
            parseInt(productContainer.find(".shopItemPrice")[0].innerHTML);
        var prID = productContainer.find(".hiddenID")[0].innerHTML;
        updateCount(prID, value);
        data = getData() || [];
        updateSum(data);
    });

    $(".shopItemCount").on('keydown', onlyNumeric);
    $(".shopItemCount").on('blur change', countInput);
    $(".shopItemCount").on('blur change',
        function () {
            value = $(this).val();
            if (!value || parseInt(value) <= 0) value = 1;
            var productContainer = $(this).parents('.shopPopupItem');
            productContainer.find(".shopItemPriceSum")[0].innerHTML = parseInt(value) *
                parseInt(productContainer.find(".shopItemPrice")[0].innerHTML);
            var prID = productContainer.find(".hiddenID")[0].innerHTML;
            updateCount(prID, value);
            data = getData() || [];
            updateSum(data);
        });


    $(".shopPopupItem .remove").click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        var productContainer = $(this).parents('.shopPopupItem');
        var prID = productContainer.find(".hiddenID")[0].innerHTML;
        C.del(prID);
        data = getData() || [];
        productContainer.remove();
        updateCartCount();
        updateSum(data);
        if ($("#cartPopup").find(".shopPopupItem").length == 0) hidePopup();
    });
    var cartPopup = $('#cartPopup');
    console.log(container.position());
    container.append(cartPopup);
    cartPopup.css("visibility", "hidden");
    $('.overlayCart, #cartPopup').show();
    cartPopup.css("right", 0);
    $('#cartPopup > .arrow').css("left", "").css("right", container.width() / 2);
    if (cartPopup.offset().left < 0) {
        cartPopup.css("right", "").css("left", 0);
        $('#cartPopup > .arrow').css("right", "").css("left", container.offset().left);
    }
    cartPopup.css("visibility", "");

    $('.closeCart, .overlayCart').click(function () {
        hidePopup();
    });

    function updateCount(ID, count) {
        var p = C.get(ID);
        p.count = count;
        C.set(ID, p);
    }

    function updateSum(data) {
        var sum = 0;
        for (i = 0; i < data.length; i++) {
            sum += data[i].price * data[i].count;
        }
        $('#cartCostValue')[0].innerHTML = sum;
    }

    $(document).keyup(function (e) {
        if (e.keyCode == 27) { // ESC
            hidePopup();
        }
    });

}

function hidePopup() {
    $('body').append($('#cartPopup'));
    $('.overlayCart, #cartPopup').hide();
    $('#cartPopup').css("right", "").css("left", "");
    isCartPopupOpened = false;
    updateCartCount();
}


function getData() {
    var productArray = [];
    var products = C.get();
    for (var x in products) {
        var p = products[x];
        if (isProduct(p)) {
            productArray.push(p);
        }
    }
    return productArray;
}

function isProduct(p) {
    return p.hasOwnProperty("name") &&
        p.hasOwnProperty("price") &&
        p.hasOwnProperty("count") &&
        p.hasOwnProperty("photoURL") &&
        p.hasOwnProperty("ID") &&
        p.hasOwnProperty("key");
}

function addOneItem(event) {
    event.preventDefault();
    var counter = $(this).parent().parent().find(".counter");
    var counterValue = parseInt(counter.val());
    if (counterValue < 99) {
        counterValue++;
        counter.val(counterValue);
    }
    var data = getData() || [];
    var ID = $(this).data('device-id');
    var item = data.filter(function (element) {
        return element.ID == ID;
    });
    if (item.length > 0) {
        item[0].count = counterValue;
        C.set(ID, JSON.stringify(item[0]));
        updateCartCount();
    }
}

function removeOneItem(event) {
    event.preventDefault();
    event.stopPropagation();
    var counter = $(this).parent().parent().find(".counter");
    var counterValue = parseInt(counter.val());
    if (counterValue > 1) {
        counterValue--;
        counter.val(counterValue);
    }
    var data = getData() || [];
    var ID = $(this).data('device-id');
    var item = data.filter(function (element) {
        return element.ID == ID;
    });
    if (item.length > 0) {
        item[0].count = counterValue;
        C.set(ID, JSON.stringify(item[0]));
        updateCartCount();
    }
}

function countInput(event) {
    var inputItem = $(this);
    if (inputItem.val().length == 0)
        inputItem.val(1);

    var data = getData() || [];
    var ID = $(this).data('device-id');
    var item = data.filter(function (element) {
        return element.ID == ID;
    });

    if (item.length > 0) {
        item[0].count = inputItem.val();
        C.set(ID, JSON.stringify(item[0]));
        updateCartCount();
    }
}

function onlyNumeric(e) {
    // Allow: backspace, delete, tab, escape, enter and .
    if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110]) !== -1 ||
        // Allow: Ctrl+A, Command+A
        (e.keyCode == 65 && (e.ctrlKey === true || e.metaKey === true)) ||
        // Allow: home, end, left, right, down, up
        (e.keyCode >= 35 && e.keyCode <= 40)) {
        // let it happen, don't do anything
        return;
    }
    // Ensure that it is a number and stop the keypress
    if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
        e.preventDefault();
    }
}
