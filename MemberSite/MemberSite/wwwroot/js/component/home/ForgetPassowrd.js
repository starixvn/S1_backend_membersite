import * as Loading from '../commJS/Loading.jsx';

(function ($, i18n, axios) {
    $("#ForgetPassowrd").submit(function(event){
        event.preventDefault();
        Loading.OnLoading();
        axios.post(`/api/Site/ForgetPwd/${langId}`, {Email:$("#email").val()})
        .then(function (response) {
            Loading.UnLoading();
            let result = response.data;
            alert(result.message);
        }).catch(function (error) {
            Loading.UnLoading();
            alert(error);
        });
    });
})(jQuery, i18n, axios);