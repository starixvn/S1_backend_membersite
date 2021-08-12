import { DataField, InputBox } from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios) {

class ResetPwdForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let dataField = new DataField("NewPassword");
        dataField.placeholder = i18n.lbl_NewPassword;
        dataField.displayName = i18n.lbl_NewPassword;
        dataField.tooltip = i18n.lbl_PasswordTooltip;
        dataField.status = 1;
        dataField.objectType = 2;
        dataField.handleCheck = this.handleCheckUserpwd.bind(this);    
        dataField.Value = "";
        dataField.AutoComplete = "off";
        formArr.push(dataField);
        // data-disabled="false"
        this.state = { formArr: formArr, SubmitDisabled: false};
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

        if(!valid) {
            Loading.UnLoading();
            this.forceUpdate();
        } else {
            axios.post('/api/AccountStrict/ResetPassword', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    alert(result.message);
                } else {
                    alert(result.message);
                }
            }).catch(function (error) {
                Loading.UnLoading();
                if (error.response.status == 401) {
                    window.location.replace(window.location.href);
                  } else {                
                    alert(error);
                  }
            });
        }
    }

    render() {
        let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);

        return (
            <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                {Inputs}
                <div className="btn-group">
                    <button className="btn btn-secondary" type="submit">
                        <div className="btn__inner">
                            <span className="text">{i18n.lbl_Submit}</span>
                        </div>
                    </button>
                </div>
            </form>
        )
    }
}

ReactDOM.render(<ResetPwdForm/>,document.getElementById('ResetPwd'));
})(jQuery, React, ReactDOM, i18n, axios);