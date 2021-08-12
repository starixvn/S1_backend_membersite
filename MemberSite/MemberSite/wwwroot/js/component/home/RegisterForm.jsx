import {DataField, InputBox} from '../commJS/InputBox.jsx';
import {getUrlParameter} from '../commJS/common';
import {getCookie} from '../commJS/Cookie';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios) {

class RegisterForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let dataField = new DataField("UserName");
        dataField.placeholder = i18n.lbl_Username;
        dataField.displayName = i18n.lbl_Username;
        dataField.tooltip = i18n.lbl_UsernameTooltip;
        dataField.status = 1;
        dataField.handleCheck = this.handleCheckUserName.bind(this);
        dataField.Value = "";
        formArr.push(dataField);

        dataField = new DataField("Userpwd");
        dataField.placeholder = i18n.lbl_Password;
        dataField.displayName = i18n.lbl_Password;
        dataField.tooltip = i18n.lbl_PasswordTooltip;
        dataField.status = 1;
        dataField.objectType = 2;
        dataField.handleCheck = this.handleCheckUserpwd.bind(this);    
        dataField.Value = "";
        formArr.push(dataField);

        dataField = new DataField("Email");
        dataField.placeholder = i18n.lbl_Email;
        dataField.displayName = i18n.lbl_Email;
        dataField.tooltip = i18n.lbl_EmailToolTip;
        dataField.status = 1;
        dataField.objectType = 4;
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
        // data-disabled="false"
        this.state = { formArr: formArr, SubmitDisabled: false, agreeterm:false};
    }
    
    async handleCheckEmail(value) {
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
                message = i18n.msg_UsernameValidate;
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
        Loading.OnLoading();
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

        if(!this.state.agreeterm)
            valid = false;

        if(!valid) {
            Loading.UnLoading();
            this.setState({SubmitDisabled: true});
        } else {
            this.setState({SubmitDisabled: false});
            data["UserLang"] = SiteLanguage;

            data["refId"] = getUrlParameter("refId")||getCookie("refId");

            data["affId"] = getUrlParameter("affId")||getCookie("affId");

            data["aff"] = getUrlParameter("aff");

            if(data["affId"] != "") {
                data["CurrencyId"]=0;
            }

            axios.post('/api/Site/Register', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    window.location.replace(result.message);
                } else {
                    alert(result.message);
                }
            }).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            });
        }
    }

  onCheckboxChange = e => {
    const key = e.target.id
    this.setState(state => ({
        ...state,
        [key]: !state[key]
    }))
  }    

    render() {
        let Inputs = this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
        let TCUrl = Check18URL + "/TermsConditions.html";
        let RRUrl = Check18URL + "/RulesandRegulations.html";

        return (
            <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                {Inputs}
                <div className='agreeterm-module'>
                    <input type="checkbox" id="agreeterm" onChange={this.onCheckboxChange} checked={this.state.agreeterm} />
                    <label className="light" htmlFor="agreeterm">
                            {i18n.lbl_Check18TC1} <a className="text-link" href={TCUrl} target="_blank"><span className="text">{i18n.lbl_TermsConditions}</span></a> {i18n.lbl_Check18TC2} <a  className="text-link" href={RRUrl} target="_blank"><span className="text">{i18n.lbl_RuleRegulations}</span></a> {i18n.lbl_Check18TC3} {siteName}. {i18n.lbl_Check18TC4}
                    </label>
                </div>
                {this.state.SubmitDisabled && ( !this.state.agreeterm) ? <div className='agreetermTip-module'><span className="text">{i18n.lbl_Check18TCTooltip}</span></div> : null}
                <div className="btn-group">
                    <button className="btn btn-secondary" data-disabled={this.state.SubmitDisabled} type="submit">
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