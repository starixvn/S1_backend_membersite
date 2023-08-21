$(function () {
    function checkpwd(value) {
        let regx = /^[a-zA-Z0-9@@$*!%#?&]*$/, status = 0, message, data = {};
        if ($.trim(value)) {
            if (value.length < 6 || value.length > 18) {
                status = 2;
                message = i18n.msg_PasswordLength;
            } else if (!regx.test(value)) {
                status = 2;
                message = i18n.msg_PasswordValidate;
            } else {
                status = 3;
                message = "";
            }
        } else {
            status = 2;
            message = i18n.msg_PasswordReq;
        }

        data["status"] = status;
        data["message"] = message;

        return data;
    }

    $("#ChangePwdForm .form-group .input-control").on("blur", function () {
        let field = $(this),
            fieldval = $.trim($(this).val()),
            data = checkpwd(fieldval);
        if (data.status !== 3) {
            field.addClass("invalid");
            field.next(".invalid-mess").html(data.message);
        } else {
            field.removeClass("invalid");
            field.next(".invalid-mess").html("");
        }
    })

    $(".repwd").on("blur", function () {
        let newieldval = $(".newpwd").val(),
            refieldval = $.trim($(this).val());
        if (refieldval) {
            if (newieldval != refieldval) {
                $(this).addClass("invalid");
                $(this).next(".invalid-mess").html("Retype And " + i18n.lbl_NewPassword + " Input is different");
            } else {
                $(this).removeClass("invalid");
                $(this).next(".invalid-mess").html("");
            }
        }
    })


    $("#ChangePwdForm").on("submit", function () {
        event.preventDefault();
        let data = {},
            OldPasswordValue = $.trim($(".oldpwd").val()),
            NewPasswordValue = $.trim($(".newpwd").val()),
            RetypePasswordValue = $.trim($(".repwd").val());

        oldstatus = checkpwd(OldPasswordValue);
        newstatus = checkpwd(NewPasswordValue);
        Retypestatus = checkpwd(RetypePasswordValue);

        console.log(RetypePasswordValue);
        console.log(Retypestatus.status);

        if (oldstatus.status === 3 && newstatus.status === 3 && Retypestatus.status === 3) {

            data["OldPassword"] = OldPasswordValue;
            data["NewPassword"] = NewPasswordValue;

            axios.post('/api/Account/ChangePassword', data)
                .then(function (response) {
                    let result = response.data;
                    if (result.errorCode == 0) {
                        alert(result.message);
                    } else {
                        alert(result.message);
                    }
                }).catch(function (error) {
                    if (error.response.status == 401) {
                        window.location.replace(window.location.href);
                    } else {
                        alert(error);
                    }
                });
        }
    })
});