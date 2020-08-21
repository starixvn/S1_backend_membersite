import { number_format } from "../../../../js/component/commJS/common";

(function ($, i18n, axios) {
	var _this = this;
	var isLogout = false;
	var _WalletInfo = null;

	$(".icon-logout").on("touchstart, click", function () {
		axios
			.post("/api/Account/SignOut")
			.then(function (response) {
				var result = response.data;
				if (result.errorCode == 0) {
					isLogout = true;
					alert(result.message);
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			})
			.catch(function (error) {
				if (error.response != null && error.response.status == 401) {
					isLogout = true;
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			});
	});

	$(".Js-logout").on("touchstart, click", function () {
		axios
			.post("/api/Account/SignOut")
			.then(function (response) {
				var result = response.data;
				if (result.errorCode == 0) {
					isLogout = true;
					alert(result.message);
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			})
			.catch(function (error) {
				if (error.response != null && error.response.status == 401) {
					isLogout = true;
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			});
	});

	$("ul.lang-list > .lang:not(.selected)").on("touchstart, click", function (
		e
	) {
		axios
			.post("/api/Account/SetLanguage", {
				LangId: $(this).attr("Js-lang-id")
			})
			.then(function (response) {
				var result = response.data;
				if (result.errorCode == 0) {
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			})
			.catch(function (error) {
				if (error.response != null && error.response.status == 401) {
					window.location.replace(window.location.href);
				} else {
					alert(result.message);
				}
			});
	});

	var onlineCheck = function () {
		axios
			.post("/api/Account/onlineCheck")
			.then(
				function (response) {
					var result = response.data;

					if (result.errorCode != null && result.errorCode != 0) {
						if (!isLogout) {
							alert(result.message);
							window.location.replace(window.location.href);
						}
					} else {
						setTimeout(onlineCheck.bind(this), 10000);
					}
				}.bind(_this)
			)
			.catch(
				function (error) {
					if (
						error.response != null &&
						error.response.status == 401
					) {
						window.location.replace(window.location.href);
					} else {
						setTimeout(onlineCheck.bind(this), 10000);
					}
				}.bind(_this)
			);
	};

	var getWalletInfo = function () {
		axios
			.post("/api/Account/GetWalletInfo")
			.then(function (response) {
				var result = response.data;
				if (result.errorCode != null && result.errorCode == 0) {
					setWalletInfo(result.data);
				} else {
					alert("Error.");
				}
			})
			.catch(function (error) {
				alert(error);
			});
	};

	var getBonusWallet = function () {
		axios
			.post("/api/account/GetCustBonusWallet")
			.then(function (response) {
				var result = response.data;
				if (result.errorCode != null && result.errorCode == 0) {
					setBonusWalletInfo(result.data);
				}
			})
			.catch(function (error) {
				alert(error);
			});
	};

	var setBonusWalletInfo = function (walletInfo) {
		walletInfo.forEach(w => {
			$(
				`<p class="balance">${
					i18n[`lbl_PromoCategory_${w.promoCategoryId}`]
				} ${number_format(w.bonus)}</p>`
			).insertAfter(".Js-Balance");
		});
	};

	var setWalletInfo = function (walletInfo) {
		_WalletInfo = walletInfo;
		if (walletInfo.isHideBalance) {
			$(".Js-Balance-Top").html("");
		} else {
			$(".Js-Balance-Top").html(number_format(walletInfo.balance));
		}
		$("#show-cash").prop("checked", !walletInfo.isHideBalance);
		$(".Js-Balance").html(
			`${number_format(
				walletInfo.balance
			)}<i class="icon icon-refresh-white"></i>`
		);

		$("#header-wallet-info > p:not(.Js-Balance)").remove();
		for (var i = walletInfo.tpWallets.length - 1; i >= 0; i--) {
			var tpName = walletInfo.tpWallets[i].tpName;
			var tpBalance = walletInfo.tpWallets[i].balance;
			$(
				`<p class="balance">${tpName} ${number_format(tpBalance)}</p>`
			).insertAfter(".Js-Balance");
		}

		getBonusWallet();
	};

	var onHideBalance = function () {
		var walletInfo = _WalletInfo;
		if (walletInfo != null) {
			axios.get("/api/Account/SetHideBalance", {
				params: { value: !walletInfo.isHideBalance }
			});
			walletInfo.isHideBalance = !walletInfo.isHideBalance;

			if (walletInfo.isHideBalance) {
				$(".Js-Balance-Top").html("");
			} else {
				$(".Js-Balance-Top").html(number_format(walletInfo.balance));
			}
		}
	};

	$(function () {
		setTimeout(onlineCheck.bind(this), 100);
		$(".Js-Currency").html(i18n.lbl_Currency);
		getWalletInfo();

		$("#show-cash").change(function () {
			onHideBalance();
		});

		$(".Js-Wallet-Refresh").on("touchstart, click", function (e) {
			getWalletInfo();
		});

		onGetMsgCount();
		window.onGetMsgCount = onGetMsgCount.bind(this);
	});

	var onGetMsgCount = function onGetMsgCount() {
		axios
			.post("/api/Account/GetMsgCount")
			.then(
				function (response) {
					var result = response.data;
					if (result.errorCode == 0) {
						var msgCount = "";
						if (result.data > 0) {
							msgCount = result.data;
							if (msgCount > 99) {
								msgCount = "99+";
							}
							$("#msg-noti-icon").addClass("has-new");
						} else {
							$("#msg-noti-icon").removeClass("has-new");
						}
					}

					setTimeout(onGetMsgCount.bind(this), 30000);
				}.bind(_this)
			)
			.catch(
				function (error) {
					setTimeout(onGetMsgCount.bind(this), 30000);
				}.bind(_this)
			);
	};
})(jQuery, i18n, axios);
