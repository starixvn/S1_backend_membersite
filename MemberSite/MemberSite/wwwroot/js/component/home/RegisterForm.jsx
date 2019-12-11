import {DataField, InputBox} from '../commJS/InputBox.jsx';
import {getUrlParameter} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios) {

class RegisterForm extends React.Component {
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

        let currencyIdDataField = new DataField("CurrencyId");
        currencyIdDataField.displayName=i18n.lbl_PrefferedCurrency;
        currencyIdDataField.tooltip=i18n.lbl_PreferredCurrencyToolTip;
        currencyIdDataField.status=1;
        currencyIdDataField.objectType=3;
        currencyIdDataField.Value=defCurrencyId;
        currencyIdDataField.dropdownList=currencyList;
        formArr.push(currencyIdDataField);
        // data-disabled="false"
        this.state = { formArr: formArr, SubmitDisabled: false};
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
            this.setState({SubmitDisabled: true});
            this.forceUpdate();
        } else {
            this.setState({SubmitDisabled: false});
            data["UserLang"] = SiteLanguage;

            data["refId"] = getUrlParameter("refId");

            axios.post('/api/Site/Register', data)
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

    render() {
        let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);

        return (
            <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                {Inputs}
                <div className="btn-group">
                    <button className="btn btn-secondary" data-disabled={this.state.SubmitDisabled}>
                        <div className="btn__inner">
                            <i className="icon icon-done-circle"></i>
                            <span className="text">{i18n.lbl_CompleteRegistration}</span>
                        </div>
                    </button>
                </div>
            </form>
        )
    }
}

ReactDOM.render(<RegisterForm/>,document.getElementById('RegisterForm'));
})(jQuery, React, ReactDOM, i18n, axios);