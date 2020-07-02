$(document).ready(function () {
  // Languages
  $(".languages").click(function () {
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

  // Toogle

  $(".toggle-section").on("click", function (e) {
    $(this).toggleClass("is-collapse");
    $(`#${$(this).data("toggle-content")}`)
      .stop()
      .slideToggle();
  });
});
