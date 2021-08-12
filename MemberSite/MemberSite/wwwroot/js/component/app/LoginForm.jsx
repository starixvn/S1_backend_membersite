import {DataField, InputBox} from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';
import {getUrlParameter} from '../commJS/common';
(function ($, React, ReactDOM, i18n, axios, window) {

class LoginForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let userNameDataField = new DataField("UserName");
        userNameDataField.placeholder = i18n.lbl_Username;
        userNameDataField.tooltip = i18n.lbl_UsernameTooltip;
        userNameDataField.status = 1;
        userNameDataField.handleCheck = this.handleCheckUserName.bind(this);
        userNameDataField.Value = "";
        formArr.push(userNameDataField);

        let userpwdDataField = new DataField("Userpwd");
        userpwdDataField.placeholder = i18n.lbl_Password;
        userpwdDataField.tooltip = i18n.lbl_PasswordTooltip;
        userpwdDataField.status = 1;
        userpwdDataField.objectType = 2;
        userpwdDataField.handleCheck = this.handleCheckUserpwd.bind(this);        
        userpwdDataField.Value = "";
        formArr.push(userpwdDataField);

        this.state = { formArr: formArr, popLoginForm: getUrlParameter("loginCont")=="1", ShowPwd: false};
    }
    
    async handleCheckUserName(value) {
        let status = 1;
        let message = "";
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
                message = i18n.msg_UsernameValidate;
            } else {
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }

    async handleCheckUserpwd(value) {
        let status = 1;
        let message = "";
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
                message = i18n.msg_PasswordValidate;
            } else {
                status = 3;
                message = "";
            }
        }

        return {status: status, message: message};
    }

    async handleSubmit(event) {
        event.preventDefault();
        this.setState({popLoginForm: false}, function() {
                Loading.OnLoading();
            }
        );
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
            Loading.UnLoading();
            this.setState({popLoginForm: true});
        } else {
            axios.post('/api/Site/SignIn', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    window.location.replace(result.message);
                } else {
                    alert(result.message);
                    this.setState({popLoginForm: true});
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.setState({popLoginForm: true});
            }.bind(this));
        }
    }

    onClose() {
        this.setState({popLoginForm: false});
    }

    componentDidMount() {
        $(".loginBtn").on("touchstart, click", function(e) {this.setState({popLoginForm: true});}.bind(this));
    }

    onChangeUserName(userNameDataField, event) {
        userNameDataField.Value=event.target.value;
        this.forceUpdate();
    }

    onChangeUserpwd(userpwdDataField, event) {
        userpwdDataField.Value=event.target.value;
        this.forceUpdate();
    }

    onShowPwd() {
        this.setState({ShowPwd:!this.state.ShowPwd});
    }

    onShowLoginForm() {
        this.setState({popLoginForm: true});
    }

    render() {
        //let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
        let userNameDataField = null;
        let userpwdDataField = null;

        this.state.formArr.forEach(function(dataField){
            if(dataField.name=="UserName") {
                userNameDataField = dataField;
            } else if(dataField.name=="Userpwd") {
                userpwdDataField = dataField;
            }
        });

        let pwdType = this.state.ShowPwd ? "text":"password";
        let ShowPwdBtn = (userpwdDataField.Value&&userpwdDataField.Value.length>0)||this.state.ShowPwd ? <a className="btn btn-icon btn-form-togglepw" data-visibility={!this.state.ShowPwd} onClick={this.onShowPwd.bind(this)}><i className="icon"></i></a>:null;
        let tooltip = [];
        this.state.formArr.forEach(function(dataField) { 
            if(dataField.status==2) { 
                tooltip.push(<div key={dataField.name}>{dataField.tooltip}</div>);
            }
        });

        return (
            <div className="login" data-open={this.state.popLoginForm}>
                <div className="overlay" onClick={this.onClose.bind(this)}></div>
                <div className="login__dialog">
                    <div className="heading">
                        <span className="text">{i18n.lbl_Login}</span>
                        <button className="btn btn-icon" onClick={this.onClose.bind(this)}>
                            <i className="icon icon-clear"></i>
                        </button>
                    </div>
                    <p>{i18n.lbl_DontHaveAccount }
                        <a className="text-link" href={joinNowUrl}><span className="text">{i18n.lbl_JoinNow}</span></a>.
                    </p>
                    <form onSubmit={this.handleSubmit.bind(this)}>
                        <div className="login__form">
                            <div className="form-group">
                                <div className="form-content" data-status={userNameDataField.dataStatus}>
                                    <div className="textfield">
                                        <input type="text" className="textfield__input" placeholder={userNameDataField.placeholder} value={userNameDataField.Value} onChange={this.onChangeUserName.bind(this, userNameDataField)}/>
                                        <span className="textfield__bg"></span>
                                    </div>
                                </div>
                            </div>
                            <div className="form-group">
                                <div className="form-content" data-status={userpwdDataField.dataStatus}>
                                    <div className="textfield">
                                        <input type={pwdType} className="textfield__input" placeholder={userpwdDataField.placeholder} value={userpwdDataField.Value} onChange={this.onChangeUserpwd.bind(this, userpwdDataField)} autoComplete="off"/>
                                        <span className="textfield__bg"></span>
                                        {ShowPwdBtn}
                                    </div>
                                </div>
                            </div>
                            <div className="tooltip tooltip--bottom" data-open={tooltip.length>0}>
                                <span className="text">{tooltip}</span>
                            </div>
                        </div>
                        <div className="login__item-btn">
                            <button className="btn btn-highlight" type="submit">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Login}</span>
                                </div>
                            </button>
                        </div>
                        {<div className="login__item-forget">
                            <a className="text-link" href={forgetPassowrdUrl}>
                                <span className="text">{i18n.lbl_ForgetPassword}</span>
                            </a>
                        </div>}
                    </form>
                </div>
            </div>
        )
    }
}

    let loginPanel = $("<div/>").get(0);
    $("body").append(loginPanel);

    let _loginForm = ReactDOM.render(<LoginForm/>, loginPanel);
    window.ShowLogin = _loginForm.onShowLoginForm.bind(_loginForm);
})(jQuery, React, ReactDOM, i18n, axios, window);