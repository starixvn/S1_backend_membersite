$(".icon-logout").on("touchstart, click", function () {
    axios.post('/api/Account/SignOut')
    .then(function (response) {
        var result = response.data;
        if (result.errorCode == 0) {
            isLogout = true;
            alert(result.message);
            window.location.replace(window.location.href);
        } else {
            alert(result.message);
        }
    }).catch(function (error) {
        if (error.response != null && error.response.status == 401) {
            isLogout = true;
            window.location.replace(window.location.href);
        } else {
            alert(result.message);
        }
    });
});

$("ul.lang-list > .lang:not(.selected)").on("touchstart, click", function (e) {
    axios.post('/api/Account/SetLanguage',{LangId:$(this).attr("Js-lang-id")})
    .then(function (response) {
        var result = response.data;
        if (result.errorCode == 0) {
            window.location.replace(window.location.href);
        } else {
            alert(result.message);
        }
    }).catch(function (error) {
        if (error.response !=null &&  error.response.status == 401) {
            window.location.replace(window.location.href);
        } else {
            alert(result.message);
        }
    });
});