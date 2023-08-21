var form = document.getElementById('loginForm');
form.onsubmit = function (e) {
    var data = {
        UserName: $("#UserName").val(),
        Userpwd: $("#Userpwd").val()
    };
    e.preventDefault();
    axios.post("/api/Site/Validation", data).then(function (response) {
        var result = response.data;

        if (result.errorCode == 0) {
            alert(result.message);
            location.reload();
        } else {
            alert(result.message);
        }
    })
        .catch(function (error) {
            alert(error);
            console.log(error);
        });
};