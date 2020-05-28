(function($, i18n, axios) {
    function getUrlParameter(name) {
        name = name.replace(/[\[]/, '\\[').replace(/[\]]/, '\\]');
        var regex = new RegExp('[\\?&]' + name + '=([^&#]*)','i');
        var results = regex.exec(location.search);
        return results === null ? '' : decodeURIComponent(results[1].replace(/\+/g, ' '));
    }

    function getCookie(cookieName) {
        var name = cookieName + "=";
        var ca = document.cookie.split(';');
        for(var i=0; i<ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0)==' ') c = c.substring(1);
            if (c.indexOf(name) == 0) return c.substring(name.length,c.length);
        }
        return "";
    }    
    
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
        let formArr = [];
        let dataField = {};
        dataField.name="UserName";
        dataField.handleCheck = handleCheckUserName.bind(this);
        formArr.push(dataField);

        dataField = {};
        dataField.name="Userpwd";        
        dataField.handleCheck = handleCheckUserpwd.bind(this);
        formArr.push(dataField);

        dataField = {};
        dataField.name="Email";
        dataField.handleCheck = handleCheckEmail.bind(this);
        formArr.push(dataField);   

        let data = {};
        let valid = true;
        for(var i=0; i < formArr.length; i++){
            let dataField = formArr[i];
            let fieldInput = $("#Register_"+dataField.name);

            let validRe = await dataField.handleCheck(fieldInput.val());
            console.log(validRe);
            if(validRe.status == 2) {
                fieldInput.addClass("invalid");
                fieldInput.next().show();
                fieldInput.next().html(validRe.message);
                valid = false;
            } else {
                fieldInput.next().hide();
                fieldInput.removeClass("invalid");
            }

            data[dataField.name] = fieldInput.val();
        }

        if(!$("#Register_agreeterm").prop("checked")) {
            valid = false;
            $("#Register_agreeterm").addClass("invalid");
        } else {
            $("#Register_agreeterm").removeClass("invalid");
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
            } else {
                data["CurrencyId"] = $("#Register_CurrencyId").val();
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