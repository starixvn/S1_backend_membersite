import { DataField, InputBox } from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {

class ProfileForm extends React.Component {
    //data-status="error" 1.info, 2.error,3.valid
    constructor(props) {
        super(props);
        let formArr=[];

        let dataField = new DataField("Email");
        dataField.placeholder = "Email";
        dataField.displayName = "Email";
        dataField.tooltip = "Email";
        dataField.status = 1;
        dataField.objectType = 4;
        dataField.handleCheck = this.handleCheckEmail.bind(this);
        dataField.Value = "";
        formArr.push(dataField);

        this.state = { formArr: formArr, Profile: null, isUpdate: false};
    }
    
    async handleCheckEmail(value) {
        let status = 1;
        let message = "tooltip";
        let regx = /(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])/;
        if(value!=null) {
            if(value.length == 0) {
                status = 2;
                message = "i18n.msg_MailReq";
            } else if(!regx.test(value)) {
                status = 2;
                message = "Mail Format is wrong";
            } else {
                let res = await axios.post('/api/Account/ExistsEmail', {Email: value});
                let data = res.data;
                if(data.errorCode==0 && data.data) {
                    status = 2;
                    message = "Mail Is Exists";
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
            axios.post('/api/Account/SetProfile', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    alert(result.message);
                    this.getCustInfoProfile();
                } else {
                    alert(result.message);
                }
            }).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            });
        }
    }

    componentDidMount() {
        this.getCustInfoProfile();
    }

    getCustInfoProfile() {
        Loading.OnLoading();
        axios.post('/api/account/GetCustInfoProfile')
        .then(function (response) {
            Loading.UnLoading();
            let result = response.data;
            if (result.errorCode == 0) {
                this.setState({Profile: result.data, isUpdate:false});
            } else {
                alert(result.message);
            }
        }.bind(this)).catch(function (error) {
            Loading.UnLoading();
            alert(error);
        }.bind(this));
    }

    onUpdate() {
        console.log("ssss");
        this.setState({isUpdate: true});
    }

    onBack() {
        this.setState({isUpdate: false});
    }

    render() {
        let Inputs = null;
        let profile = this.state.Profile;        
        let isUpdate = this.state.isUpdate;
        if(isUpdate) {
            Inputs = this.state.formArr.map(function(dataField) { 
                if(dataField.name=="Email") {
                    dataField.Value=profile.Email;
                }
                return <InputBox key={dataField.name} DataField={dataField}/>;
            });
        }
        return (
            profile!=null ?<div>
                <div className="heading">
                    <span className="text">{"Profile"}</span>
                </div>
                <form className="form" onSubmit={isUpdate ? this.handleSubmit.bind(this): null}>
                    <div className="form-group">
                        <label className="form-title"><b>User Name</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{profile.userName}</span>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title"><b>Vip Level</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{profile.userLevel}</span>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title"><b>Agent</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{profile.agent}</span>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title"><b>Register  Time</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{moment(profile.regTime).format('l LTS')}</span>
                        </div>
                    </div>                    
                    <div className="form-group">
                        <label className="form-title"><b>Last Login Time</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{moment(profile.lastLoginTime).format('l LTS')}</span>
                        </div>
                    </div>
                    {isUpdate ? null: <div className="form-group">
                        <label className="form-title"><b>Email</b></label>
                        <div className="form-content form-content--text">
                                <span className="text">{profile.email}</span>
                        </div>
                    </div>}                             
                    {Inputs}
                    {isUpdate ? 
                    <div className="btn-group">
                        <button className="btn btn-primary" type="button" onClick={this.onBack.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">Back</span>
                            </div>
                        </button>                    
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_Submit}</span>
                            </div>
                        </button>
                    </div>: <div className="btn-group">
                        <button className="btn btn-secondary" type="button" onClick={this.onUpdate.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">Update</span>
                            </div>
                        </button>
                    </div>}
                </form>
            </div>:null
        )
    }
}

ReactDOM.render(<ProfileForm/>,document.getElementById('Profile'));
})(jQuery, React, ReactDOM, i18n, axios, moment);