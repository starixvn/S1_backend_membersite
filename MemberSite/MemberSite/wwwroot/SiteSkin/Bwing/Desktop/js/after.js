(function($, i18n, axios) {
    var _this = this;
    var isLogout = false;
    var _WalletInfo = {};

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

    var onlineCheck = function() {
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

    var getWalletInfo = function() {
        axios.post('/api/Account/GetWalletInfo')
        .then(
            function(response) {
                var result = response.data;
                if (result.errorCode != null && result.errorCode == 0) {
                    setWalletInfo(result.data);
                } else {
                    alert('Error.');    
                }
            }).catch(function (error) {
                alert(error);
        });
    };
    
    var setWalletInfo = function(walletInfo) {
        if(walletInfo.isHideBalance) {
            $(".Js-Balance-Top").html('');
        } else {
            $(".Js-Balance-Top").html(walletInfo.balance);
        }
        $("#show-cash").prop('checked', !walletInfo.isHideBalance);
        $(".Js-Balance").html(walletInfo.balance);
        $(".Js-LastLoginTime").html(moment(walletInfo.lastLoginTime).format('l LTS'));
        $(".Js-LastTransaction").html(moment(walletInfo.lastTransaction).format('l LTS'));
        $(".dropdown-wallet > .wallet-item:not(.wallet-main)").remove();
        for(var i = walletInfo.tpWallets.length -1; i >= 0; i--){
            var tpName = walletInfo.tpWallets[i].tpName;
            var tpBalance =  walletInfo.tpWallets[i].balance;
            $(`<div class="wallet-item"><label>${tpName}</label><span>${tpBalance}</span></div>`).insertAfter(".dropdown-wallet > .wallet-item.wallet-main");        
        }
    }    

    var onHideBalance = function() {
        let walletInfo = _WalletInfo;
        if(walletInfo != null) {
            axios.get('/api/Account/SetHideBalance',{ params: {value:!walletInfo.isHideBalance} });
            walletInfo.isHideBalance = !walletInfo.isHideBalance;
            
            if(walletInfo.isHideBalance) {
                $(".Js-Balance-Top").html('');
            } else {
                $(".Js-Balance-Top").html(walletInfo.balance);
            }
        }
    }    

    $(function() {
        setTimeout(onlineCheck.bind(this), 100);
        $(".Js-Currency").html(i18n.lbl_Currency);
        getWalletInfo();

        $("#show-cash").change(function() {
            onHideBalance();
        });

        $(".Js-Wallet-Refresh").on("touchstart, click", function (e) {
            getWalletInfo();
        });
    });
})(jQuery, i18n, axios);