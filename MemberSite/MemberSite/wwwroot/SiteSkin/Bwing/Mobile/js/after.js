(function($, i18n, axios) {
    var _this = this;
    var isLogout = false;

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

    $(".Js-logout").on("touchstart, click", function () {
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

    var onlineCheck = function onlineCheck() {
        axios
        .post("/api/Account/onlineCheck")
        .then(
        function(response) {
            var result = response.data;

            if (result.errorCode != null && result.errorCode != 0) {
                if(!isLogout){
                alert(result.message);
                window.location.replace(window.location.href);
                }
            } else {
            setTimeout(onlineCheck.bind(this), 10000);
            }
        }.bind(_this)
        )
        .catch(
        function(error) {
            if (error.response!=null && error.response.status == 401) {
            window.location.replace(window.location.href);
            } else {
            setTimeout(onlineCheck.bind(this), 10000);
            }
        }.bind(_this)
        );
    };

    $(function() {
        setTimeout(onlineCheck.bind(this), 100);
    });
})(jQuery, i18n, axios);