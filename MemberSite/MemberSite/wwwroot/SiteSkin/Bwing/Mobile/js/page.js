$(document).ready(function () {
  // Languages
  $(".languages-box").click(function () {
    $(this).toggleClass("open");
  });
  $("ul.lang-list li").click(function () {
    $("ul.lang-list li").removeClass("selected");
    $(this).addClass("selected");
    if ($(this).hasClass("lang-en")) {
      $(".language-dropdown")
        .find(".lang-flag")
        .addClass("lang-en")
        .removeClass("lang-vn");
      $(".lang_selected").html("<p>English</p>");
    } else {
      $(".language-dropdown")
        .find(".lang-flag")
        .addClass("lang-vn")
        .removeClass("lang-en");
      $(".lang_selected").html("<p>Tiếng Việt</p>");
    }
    $(this).removeClass("open");
  });
  $("#btn-menu").click(function() {
    $(".nav-mobile-overlay").addClass("show");
  });

  $("#btn-menu-close").click(function() {
    $(".nav-mobile-overlay").removeClass("show");
  });

  $(".btn-login").click(function() {
    $('[data-remodal-id=modal-login]').remodal();
  });

  $("#is-login").click(function () {
    $(".header").addClass("is-login");
    $(".wrap-content").addClass("is-login")
  });  
  
});
