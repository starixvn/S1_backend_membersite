import { DataField, InputBox } from '../commJS/InputBox.jsx';

(function ($, React, ReactDOM, i18n, axios) {

class ProfileForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let emailDataField = new DataField("Email");
        emailDataField.placeholder = i18n.lbl_Username;
        emailDataField.displayName = i18n.lbl_Username;
        emailDataField.tooltip = i18n.lbl_UsernameTooltip;
        emailDataField.status = 1;
        emailDataField.objectType = 4;
        emailDataField.handleCheck = this.handleCheckEmail.bind(this);
        emailDataField.Value = "";
        formArr.push(emailDataField);

        this.state = { formArr: formArr};
    }
    
    async handleCheckEmail(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = i18n.msg_UsernameReq;
            } else if(!regx.test(value)) {
                status = 2;
                message = i18n.msg_UsernameVaildate;
            } else {
                let res = await axios.post('/api/Account/ExistsEmail', {Email: value});
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
            axios.post('/api/Account/SetProfile', data)
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {
                    alert(result.message);
                    location.reload();
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
                    <button type="submit" value="submit">{"i18n.lbl_CompletedRegistration"}</button>
                </div>
            </form>
        )
    }
}

ReactDOM.render(<ProfileForm/>,document.getElementById('Profile'));
})(jQuery, React, ReactDOM, i18n, axios);