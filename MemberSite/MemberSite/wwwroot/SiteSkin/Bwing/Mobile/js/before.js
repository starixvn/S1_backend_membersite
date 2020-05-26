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

$("#RegisterForm").submit(function(event) {
  event.preventDefault();

});