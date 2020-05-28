(function($, i18n, axios) {
    $("#LoginForm").submit(function(event) {
        event.preventDefault();
        let data = {UserName:$("#Login_UserName").val() ,Userpwd:$("#Login_Userpwd").val()};
        axios.post('/api/Site/SignIn', data)
        .then(function (response) {
            let result = response.data;
            if (result.errorCode == 0) {
                window.location.replace(location.href);
            } else {
                alert(result.message);
            }
        }.bind(this)).catch(function (error) {
            alert(error);
        }.bind(this));
    });
    
    async function handleCheckEmail(value) {
        let status = 1;
        let message = "";
        let regx = /(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = i18n.msg_MailReq;
            } else if(!regx.test(value)) {
                status = 2;
                message = i18n.msg_MailWrongFormat;
            } else {
                let res = await axios.post('/api/Site/ExistsEmail', {Email: value});
                let data = res.data;
                if(data.errorCode==0 && data.data) {
                    status = 2;
                    message = i18n.msg_MailExisted;
                } else {
                    status = 3;
                    message = "";
                }
            }
        }

        return {status: status, message: message};
    }

    async function handleCheckUserName(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /^[a-zA-Z0-9]*$/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = i18n.msg_UsernameReq;
            } else if(value.length < 6 || value.length > 18) {
                status = 2;
                message = i18n.msg_UsernameLength;
            } else if(!regx.test(value)) {
                status = 2;
                message = i18n.msg_UsernameVaildate;
            } else {
                let res = await axios.post('/api/Site/ExistsUserName', {UserName: value});
                let data = res.data;
                if(data.errorCode==0 && data.data) {
                    status = 2;
                    message = i18n.msg_UsernameNotAvailable;
                } else {
                    status = 3;
                    message = "";
                }
            }
        }

        return {status: status, message: message};
    }

    async function handleCheckUserpwd(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /^[a-zA-Z0-9@$*!%#?&]*$/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = i18n.msg_PasswordReq;
            } else if(value.length < 6 || value.length > 18) {
                status = 2;
                message = i18n.msg_PasswordLength;
            } else if(!regx.test(value)) {
                status = 2;
                message = i18n.msg_PasswordVaildate;
            } else {
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }    

    $("#RegisterForm").submit(async function(event) {
        event.preventDefault();

        let dataField = new DataField("UserName");
        dataField.handleCheck = this.handleCheckUserName.bind(this);
        dataField.Value = "";
        formArr.push(dataField);

        dataField = new DataField("Userpwd");
        dataField.status = 1;
        dataField.Value = "";
        formArr.push(dataField);

        dataField = new DataField("Email");
        dataField.handleCheck = this.handleCheckEmail.bind(this);
        dataField.Value = "";
        formArr.push(dataField);   

        if(getUrlParameter("affId") == "" && getCookie("affId") == ""){
            dataField = new DataField("CurrencyId");
            dataField.displayName = i18n.lbl_PrefferedCurrency;
            dataField.tooltip = i18n.lbl_PreferredCurrencyToolTip;
            dataField.status = 1;
            dataField.objectType = 3;
            dataField.Value = defCurrencyId;
            dataField.dropdownList = currencyList;
            formArr.push(dataField);
        }

        let data = {};
        let valid = true;
        for(var i=0; i < this.state.formArr.length; i++){
            let dataField = this.state.formArr[i];
            await dataField.onCheck();
            if(valid) {
                if(dataField.status == 2) {
                    valid = false;
                }
            }
            data[dataField.name] = dataField.Value;
        }

        if(!valid) {
            //Loading.UnLoading();
        } else {
            data["UserLang"] = SiteLanguage;

            data["refId"] = getUrlParameter("refId")||getCookie("refId");

            data["affId"] = getUrlParameter("affId")||getCookie("affId");

            data["aff"] = getUrlParameter("aff");

            if(data["affId"] != "") {
                data["CurrencyId"] = 0;
            }

            axios.post('/api/Site/Register', data)
            .then(function (response) {
                //Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    window.location.replace(result.message);
                } else {
                    alert(result.message);
                }
            }).catch(function (error) {
                //Loading.UnLoading();
                alert(error);
            });
        }
    });

$(function() {
    $(".loginBtn").attr("data-remodal-target", "modal-login");
});

})(jQuery, i18n, axios);