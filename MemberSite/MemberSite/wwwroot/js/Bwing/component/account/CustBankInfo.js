$(".form-account").addClass("hide");
$(".tbl-account").addClass("hide");
$(".form-add-user").addClass("hide");

let BankAccount = [];

$(function () {
	GetCustBankInfo();

	$("#BankInfoSysList").on("click", ".selected-text", function () {
		var $this = $(this);
		var dropdownContainer = $this.next(".dropdown-container");
		dropdownContainer.toggleClass("hide");
		$this.children(".icon-action").toggleClass("active");
	});
	$("#BankInfoSysList").on("click", ".dropdown-container .item", function () {
		var $this = $(this);
		var dropdownContainer = $this.parent();
		const selectedText = $("#" + dropdownContainer.data("id"));
		selectedText
			.children(".name")
			.attr("src", $this.children(".name").attr("src"));
		selectedText.children(".value").text($this.children(".value").text());
		dropdownContainer.addClass("hide");
		selectedText.children(".icon-action").toggleClass("active");
		selectedText.data("value", $this.data("value"));
	});

	$("#Bank_Account_List").on("click", ".icon-delete", function () {
		let bankInfo = BankAccount[$(this).data("bankinfo")];
		if (confirm(i18n.lbl_CheckConfirm) && bankInfo) {
			axios
				.post("/api/AccountStrict/DelCustBankInfo", bankInfo)
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							alert(result.message);
							GetCustBankInfo();
						} else {
							alert(result.message);
						}
						Loading.UnLoading();
					}.bind(this)
				)
				.catch(
					function (error) {
						Loading.UnLoading();
						alert(error);
					}.bind(this)
				);
		}
	});

	$("#btn-add-username").click(function () {
		addAccount();
	});

	$("#btn-back").click(function () {
		GetCustBankInfo();
	});

	$("#SetAccountName").on("blur", function () {
		let field = $(this),
			fieldval = $.trim($(this).val());
		if (!fieldval) {
			field.addClass("invalid");
			field.next(".invalid-mess").html(i18n.msg_NeedAccountName);
		} else {
			field.removeClass("invalid");
			field.next(".invalid-mess").html("");
		}
	});

	$("#BankNumber").on("blur", function () {
		let field = $(this),
			fieldval = $.trim($(this).val());
		if (!fieldval) {
			field.addClass("invalid");
			field.next(".invalid-mess").html(i18n.msg_NeedAccountNo);
		} else {
			field.removeClass("invalid");
			field.next(".invalid-mess").html("");
		}
	});

	let submit = false;

	$("#form").on("submit", function () {
		event.preventDefault();
		if (submit) {
			return false;
		}

		let data = {},
			AccountName = $.trim($("#SetAccountName").val()),
			SysId = parseInt(
				$.trim($("#BankInfoSysList .selected-text").data("value"))
			),
			BankNumber = $.trim($("#BankNumber").val());

		if (!BankNumber) {
			$("#BankNumber").addClass("invalid");
			$("#BankNumber").next(".invalid-mess").html(i18n.msg_NeedAccountNo);
			return false;
		}

		if (AccountName && SysId && BankNumber) {
			submit = true;
			data["AccountName"] = AccountName;
			data["SysId"] = SysId;
			data["AccountNo"] = BankNumber;

			axios
				.post("/api/AccountStrict/SetCustBankInfo", data)
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							submit = false;
							alert(result.message);
							GetCustBankInfo();
						} else {
							alert(result.message);
						}
					}.bind(this)
				)
				.catch(function (error) {
					alert(error);
				});
		}
	});

	function formatState(opt) {
		if (!opt.id) {
			return opt.text;
		}
		var optimage = $(opt.element).attr("data-image");
		if (!optimage) {
			return opt.text;
		} else {
			var $opt = $(
				'<span class="bank"><img src="' +
					optimage +
					'"/>' +
					opt.text +
					"</span>"
			);
			return $opt;
		}
	}

	function GetCustBankInfo() {
		axios
			.post("/api/Account/GetCustBankInfo")
			.then(
				function (response) {
					let result = response.data,
						total = 0;
					if (result.errorCode == 0) {
						if (result.data) {
							let list = "";
							$.each(result.data.bankInfoDetail, function (
								key,
								value
							) {
								BankAccount[value.sysId] = value;
								list +=
									"<tr>" +
									'<td class="text-center"><img src="/common/images/BankLogo/' +
									value.bankLogo +
									'" alt="vcb-logo" /></td>' +
									'<td class= "text-left">' +
									'<p class="bank-name">' +
									value.bankName +
									"</p>" +
									"<p>" +
									value.accountNo +
									"</p>" +
									"</td >" +
									'<td class="text-center"><span class="icon icon-delete" data-bankInfo="' +
									value.sysId +
									'"></span></td>' +
									"</tr >";
								total++;
							});
							$(".AccountName")
								.val(result.data.accountName)
								.attr("disabled", "true");
							$(".AccountName").after(
								'<i class="icon icon-lock"></i>'
							);
							$("#Bank_Account_List").html(list);

							if (total >= 3) {
								$("#btn-add-username").hide();
							} else {
								$("#btn-add-username").show();
							}

							$(".form-account").removeClass("hide");
							$(".tbl-account").removeClass("hide");
							$(".form-add-user").addClass("hide");
						} else {
							addAccount();
							$("#btn-back").hide();
						}
					} else {
						alert(result.message);
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
				}.bind(this)
			);
	}

	function GetBankInfoSysList() {
		$("#BankNumber").val("");
		axios
			.post("/api/Payment/GetBankInfoSysList")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						if (result.data && result.data.length > 0) {
							let BankInfo =
								'<div class="selected-text input-control" id="id-1" data-value="' +
								result.data[0].sysId +
								'"><img src ="/common/images/BankLogo/' +
								result.data[0].bankLogo +
								'" alt="acb-logo" class="name"><span class="value">' +
								result.data[0].bankName +
								'</span><span class="icon-action"><i class="icon icon-arrow-down"></i></span></div>';
							BankInfo +=
								'<div class="dropdown-container hide" data-id="id-1">';
							$.each(result.data, function (key, value) {
								BankInfo +=
									'<div class="item" data-value="' +
									value.sysId +
									'"><img src="/common/images/BankLogo/' +
									value.bankLogo +
									'" alt="acb-logo" class="name" /><span class="value">' +
									value.bankName +
									"</span></div>";
							});
							BankInfo += "</div>";
							$("#BankInfoSysList").html(BankInfo);
						}
					} else {
						alert(result.message);
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
				}.bind(this)
			);
	}

	function addAccount() {
		GetBankInfoSysList();
		$(".form-account").addClass("hide");
		$(".form-add-user").removeClass("hide");
		$(".tbl-account").addClass("hide");
	}
});
