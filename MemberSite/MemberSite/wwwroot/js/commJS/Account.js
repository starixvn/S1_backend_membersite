"use strict";
(function($, i18n, axios) {
  var _this = this;
  var isLogout = false;

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
  
  var onGetMsgCount = function onGetMsgCount() {
    axios
      .post("/api/Account/GetMsgCount")
      .then(
        function(response) {
          var result = response.data;
          if (result.errorCode == 0) {
            var msgCount = "";
            if(result.data > 0) {
                msgCount = result.data;
                if(msgCount>99){
                  msgCount="99+";
                }
                $(".MsgCount").html(msgCount).show();
            } else {
                $(".MsgCount").html(msgCount).hide();
            }
          }

          setTimeout(onGetMsgCount.bind(this), 30000);
        }.bind(_this)
      )
      .catch(
        function(error) {
          setTimeout(onGetMsgCount.bind(this), 30000);
        }.bind(_this)
      );
  };

  $(function() {
    setTimeout(onlineCheck.bind(this), 1000);
    onGetMsgCount();
    window.onGetMsgCount = onGetMsgCount.bind(this);

    $(".logoutBtn").on("touchstart, click", function () {
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
            if (error.response !=null &&  error.response.status == 401) {
                isLogout = true;
                window.location.replace(window.location.href);
            } else {
                alert(result.message);
            }
        });
    });

    $(".dropdown__item[data-lang-id]").on("touchstart, click", function (e) {
        axios.post('/api/Account/SetLanguage',{LangId:$(this).attr("data-lang-id")})
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
  });
})(jQuery, i18n, axios);