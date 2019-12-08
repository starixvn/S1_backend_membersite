"use strict";
(function($, i18n, axios) {
  var _this = this;

  var onlineCheck = function onlineCheck() {
    axios
      .post("/api/Account/onlineCheck")
      .then(
        function(response) {
          var result = response.data;

          if (result.errorCode != null && result.errorCode != 0) {
            alert(result.message);
            window.location.replace(window.location.href);
          } else {
            setTimeout(onlineCheck.bind(this), 10000);
          }
        }.bind(_this)
      )
      .catch(
        function(error) {
          if (error.response.status == 401) {
            window.location.replace(window.location.href);
          } else {
            setTimeout(onlineCheck.bind(this), 10000);
          }
        }.bind(_this)
      );
  };

  var getUserBalance = function getUserBalance() {
    axios
      .post("/api/Account/GetBalance")
      .then(
        function(response) {
          var result = response.data;
          if (result.errorCode != null && result.errorCode == 0) {
            $(".account-info__amount").html(number_format(result.data.toFixed(2)));
          } else {

          }
        }.bind(_this)
      )
      .catch(
        function(error) {
          if (error.response !=null &&  error.response.status == 401) {
            window.location.replace(window.location.href);
          }
        }.bind(_this)
      );
  };  

  $(function() {
    setTimeout(onlineCheck.bind(this), 1000);
    getUserBalance();

    $("#logoutBtn").click(function () {
        axios.post('/api/Account/SignOut')
        .then(function (response) {
            let result = response.data;
            if (result.errorCode == 0) {
                alert(result.message);
                window.location.replace(window.location.href);
            } else {
                alert(result.message);
            }
        }).catch(function (error) {
            alert(error);
        });
    });

  });
})(jQuery, i18n, axios);