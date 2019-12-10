import {DataField, InputBox} from '../commJS/InputBox.jsx';
import {getUrlParameter} from '../commJS/common';
(function ($, React, ReactDOM, i18n, axios) {

class LoginForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let userNameDataField = new DataField("UserName");
        userNameDataField.placeholder=i18n.lbl_Username;
        userNameDataField.displayName=i18n.lbl_Username;
        userNameDataField.tooltip=i18n.lbl_UsernameTooltip;
        userNameDataField.status=1;
        userNameDataField.handleCheck = this.handleCheckUserName.bind(this);
        userNameDataField.Value="";
        formArr.push(userNameDataField);

        let userpwdDataField = new DataField("Userpwd");
        userpwdDataField.placeholder=i18n.lbl_Password;
        userpwdDataField.displayName=i18n.lbl_Password;
        userpwdDataField.tooltip=i18n.lbl_PasswordTooltip;
        userpwdDataField.status=1;
        userpwdDataField.objectType=2;
        userpwdDataField.handleCheck = this.handleCheckUserpwd.bind(this);        
        userpwdDataField.Value="";
        formArr.push(userpwdDataField);

        this.state = { formArr: formArr, popLoginForm: getUrlParameter("loginCont")=="1"};
    }
    
    async handleCheckUserName(value) {
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
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }

    async handleCheckUserpwd(value) {
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

    async handleSubmit(event) {
        event.preventDefault();
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
            this.forceUpdate();
        } else {
            axios.post('/api/Site/SignIn', data)
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {
                    window.location.replace(result.message);
                } else {
                    alert(result.message);
                }
            }).catch(function (error) {
                alert(error);
            });
        }
    }

    onClose() {
        this.setState({popLoginForm: false});
    }

    componentDidMount() {
        $("#loginBtn1").on("touchstart, click", function(e) {this.setState({popLoginForm: true});}.bind(this));
        $("#loginBtn2").on("touchstart, click", function(e) {this.setState({popLoginForm: true});}.bind(this));
        $("#loginBtn3").on("touchstart, click", function(e) {this.setState({popLoginForm: true});}.bind(this));
    }

    render() {
        let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);

        return (
            <div className="login" data-open={this.state.popLoginForm}>
                <div className="overlay" onClick={this.onClose.bind(this)}></div>
                <div className="login__dialog">
                    <div className="heading">
                        <span className="text">Login</span>
                        <button className="btn btn-icon" onClick={this.onClose.bind(this)}>
                            <i className="icon icon-clear"></i>
                        </button>
                    </div>
                    <p>Don't have an account? 
                        <a className="text-link" href={joinNowUrl}><span className="text">Join Now</span></a>.
                    </p>
                    <form id="loginForm" onSubmit={this.handleSubmit.bind(this)}>                                        
                        <div className="login__form">
                            {Inputs}
                        </div>
                        {/*<div className="login__item">
                            <label className="checkbox">
                                <input type="checkbox"/>
                                <i className="icon icon-checkbox"></i>
                                <span className="text">Remember Me</span>
                            </label>
                        </div>*/}
                        <div className="login__item-btn">
                            <button type="submit" className="btn btn-highlight">
                                <span className="text">Login</span>
                            </button>
                        </div>
                    </form>
                    <div className="login__item-forget">
                        <a className="text-link">
                            <span className="text">Forget your ID or password?</span>
                        </a>
                    </div>
                </div>
            </div>
        )
    }
}

let loginPanel = $("<div/>").get(0);
$("body").append(loginPanel);

ReactDOM.render(<LoginForm/>, loginPanel);
})(jQuery, React, ReactDOM, i18n, axios);