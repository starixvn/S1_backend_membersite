(function ($, i18n, axios) {
    let onlineCheck = () => {
        axios.post('/api/Account/onlineCheck')
        .then(function (response) {
            let result = response.data;
            if (result.errorCode !=null && result.errorCode != 0) {
                alert(result.message);
                window.location.replace(window.location.href);
            } else {
                setTimeout(onlineCheck.bind(this) ,10000);
            }
        }.bind(this)).catch(function (error) {
            if(error.response.status==401){
                window.location.replace(window.location.href);
            } else {
                setTimeout(onlineCheck.bind(this) ,10000);
            }
        }.bind(this));
    }

    $(function(){ 
        setTimeout(onlineCheck.bind(this) ,1000);
    });
})(jQuery, i18n, axios);