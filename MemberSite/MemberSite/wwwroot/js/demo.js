$(function(){
    // Dropdown toggle
    $(".dropdown > .btn").on("click", function(){
        $(this).closest(".form-content").find(".tooltip").removeAttr("data-open");
        var $dropdown = $(this).parent();
        $dropdown.attr("data-open") == "true" ? $dropdown.removeAttr("data-open") : $dropdown.attr("data-open","true");
    });

    // Form tooltip toggle
    $(".btn-form-status").on("touchstart, click", function(e){
        e.stopPropagation();
        $(this).closest(".form-content").find(".dropdown").removeAttr("data-open");
        var $tooltip = $(this).closest(".form-content").find(".tooltip");
        $tooltip.attr("data-open") == "true" ? $tooltip.removeAttr("data-open") : $tooltip.attr("data-open","true");
    });

    // Form open tooltip
    $(".textfield__input").focus(function(){
        $(this).closest(".form-content").find(".tooltip").attr("data-open", "true");
    });

    // Login - open
    $("[demo-open-login]").on("click", function(){
        $(".overlay").attr("data-open", "true");
        $(".login").attr("data-open", "true");
    });

    // Login - close
    $("[demo-close-login], .overlay").on("click", function(){
        $(".overlay").removeAttr("data-open");
        $(".login").removeAttr("data-open");
    });

    // Login - tooltip
    $("[demo-login-tips]").on("touchstart, click", function() {
        $(".login__form .tooltip").attr("data-open") == "true" ? $(".login__form .tooltip").removeAttr("data-open") : $(".login__form .tooltip").attr("data-open","true");
        $(".login__form .form-content").attr("data-status") == "error" ? $(".login__form .form-content").removeAttr("data-status") : $(".login__form .form-content").attr("data-status","error");
    });
});