import { DataField, InputBox } from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';
import areaCode from '../commJS/areaCode';

(function ($, React, ReactDOM, i18n, axios, moment) {

    class ProfileData extends React.Component {
        constructor(props) {
            super(props);
        }

        async handleSubmit(event) {
            event.preventDefault();
            Loading.OnLoading();
            let data = {};
            let valid = true;

            if (!valid) {
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
                    }.bind(this)).catch(function (error) {
                        Loading.UnLoading();
                        alert(error);
                    }.bind(this));
            }
        }

        render() {
            let profile = this.state.Profile;

            return (
                <form className="form form--list" onSubmit={isUpdate ? this.handleSubmit.bind(this) : null}>

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

    class ProfileForm extends React.Component {
        _dataStatus = { "1": "info", "2": "error", "3": "valid", "4": "readonly" }
        constructor(props) {
            super(props);
            this.state = { CountryCode: this.props.Profile.countryCode || perCountry, AccountName: this.props.Profile.accountName, Email: this.props.Profile.email, PhoneNo: this.props.Profile.phoneNo, IsOpenMailTip: false, IsOpenAccountTip: false, IsOpenCountryTip: false, IsOpenPhoneTip: false};
        }

        onChange(event) {
            let obj = this.state;
            obj[event.target.name] = event.target.value;
            this.setState(obj);
        }

        async handleCheckEmail(value) {
            let status = 1;
            let message = "";
            let regx = /(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|"(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])/;
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = i18n.msg_MailReq;
                } else if (!regx.test(value)) {
                    status = 2;
                    message = i18n.msg_Wrong;
                } else {
                    let res = await axios.post('/api/Account/ExistsEmail', { Email: value });
                    let data = res.data;
                    if (data.errorCode == 0 && data.data) {
                        status = 2;
                        message = i18n.msg_MailExisted;
                    } else {
                        status = 3;
                        message = "";
                    }
                }
            }

            return { status: status, message: message };
        }

        handleCheckPhoneNo(value) {
            let status = 1;
            let message = "";
            let regx = new RegExp(areaCode[this.state.CountryCode].Pattern, "g");
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = i18n.msg_MailReq;
                } else if (!regx.test(value)) {
                    status = 2;
                    message = i18n.msg_Wrong;
                } else {
                    if (data.errorCode == 0 && data.data) {
                        status = 2;
                        message = i18n.msg_MailExisted;
                    } else {
                        status = 3;
                        message = "";
                    }
                }
            }

            return { status: status, message: message };
        }

        getCustInfoProfile() {
            this.props.getCustInfoProfile();
        }

        async handleSubmit(event) {
            event.preventDefault();
            Loading.OnLoading();
            let data = { CountryCode: this.state.CountryCode, PhoneNo: this.state.PhoneNo, AreaCode: areaCode[this.state.CountryCode].AreaCode, AccountName: this.state.AccountName, Email: this.state.Email };
            let valid = true;

            if (!valid) {
                Loading.UnLoading();
                this.forceUpdate();
            } else {
                axios.post('/api/AccountStrict/SetProfile', data)
                    .then(function (response) {
                        Loading.UnLoading();
                        let result = response.data;
                        if (result.errorCode == 0) {
                            alert(result.message);
                            this.getCustInfoProfile();
                        } else {
                            alert(result.message);
                        }
                    }.bind(this)).catch(function (error) {
                        Loading.UnLoading();
                        alert(error);
                    }.bind(this));
            }
        }

        onClickMailTip() {
            this.setState({IsOpenMailTip: !this.state.IsOpenMailTip});
        }

        onClickAccountTip() {
            this.setState({IsAccountMailTip: !this.state.IsAccountMailTip});
        }

        onClickCountryTip() {
            this.setState({IsOpenCountryTip: !this.state.IsOpenCountryTip});
        }

        onClickPhoneTip() {
            this.setState({IsOpenPhoneTip: !this.state.IsOpenPhoneTip});
        }

        render() {
            let country = <select disabled={this.props.Profile.countryCode != null} onChange={this.props.Profile.countryCode == null ? this.onChange.bind(this) : null} name="CountryCode" required="required" className="textfield__input" value={this.state.CountryCode}>{Country.map(function (item, index) {
                return <option key={item.Code} value={item.Code}>{item.CountryName}</option>;
            })}</select>;

            return (
                <form className="form form--list" onSubmit={this.handleSubmit.bind(this)}>
                    <div className="form-group">
                        <label className="form-title">{i18n.lbl_Email}</label>
                        <div className="form-content" data-status={this.props.Profile.email == null ? this._dataStatus[1] : this._dataStatus[4]}>
                            <div className="textfield">
                                <input type="email" value={this.state.Email} onChange={this.props.Profile.email == null ? this.onChange.bind(this) : null} className="textfield__input" name="Email" required />
                                <span className="textfield__bg"></span>
                                <a className="btn btn-icon btn-form-status" onClick={this.props.Profile.email == null ? this.onClickMailTip.bind(this) : null }>
                                    <i className="icon icon--lg"></i>
                                </a>
                            </div>
                            <div className="tooltip tooltip--bottom-right" data-open={this.state.IsOpenMailTip}>
                                <span className="text">{i18n.lbl_EmailToolTip}</span>
                            </div>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title">{i18n.lbl_RealName}</label>
                        <div className="form-content" data-status={this.props.Profile.accountName == null ? this._dataStatus[1] : this._dataStatus[4]}>
                            <div className="textfield">
                                <input type="text" value={this.state.AccountName} onChange={this.props.Profile.accountName == null ? this.onChange.bind(this) : null} className="textfield__input" name="AccountName" required />
                                <span className="textfield__bg"></span>
                                <a className="btn btn-icon btn-form-status" onClick={this.props.Profile.accountName == null ? this.onClickAccountTip.bind(this) : null }>
                                    <i className="icon icon--lg"></i>
                                </a>
                            </div>
                            <div className="tooltip tooltip--bottom-right" data-open={this.state.IsAccountMailTip}>
                                <span className="text">{i18n.lbl_BankAccountNameHint}</span>
                            </div>
                            <div className="form-content__tips">
                                <span className="text-accent">{i18n.lbl_RemindForName}</span>
                            </div>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title">{i18n.lbl_Country}</label>
                        <div className="form-content" data-status={this.props.Profile.countryCode == null ? this._dataStatus[1] : this._dataStatus[4]}>
                            <div className="textfield">
                                {country}
                                <span className="textfield__bg"></span>
                                <a className="btn btn-icon btn-form-status" onClick={this.props.Profile.country == null ? this.onClickCountryTip.bind(this) : null }>
                                    <i className="icon icon--lg"></i>
                                </a>
                            </div>
                            <div className="tooltip tooltip--bottom-right" data-open={this.state.IsOpenCountryTip}>
                                <span className="text">{i18n.lbl_CountryTooltip}</span>
                            </div>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title">{i18n.lbl_Phone}</label>
                        <div className="form-content form-content--mobile" data-status={this.props.Profile.phoneNo == null ? this._dataStatus[1] : this._dataStatus[4]}>
                            <div className="dropdown dropdown-default">
                                <span className="btn">
                                    <span className="text">{areaCode[this.state.CountryCode].AreaCode}</span>
                                </span>
                            </div>
                            <div className="textfield">
                                <input type="text" className="textfield__input" name="PhoneNo"  value={this.state.PhoneNo} onChange={this.props.Profile.phoneNo == null ? this.onChange.bind(this) : null} required />
                                <span className="textfield__bg"></span>
                                <a className="btn btn-icon btn-form-status" onClick={this.props.Profile.phoneNo == null ? this.onClickPhoneTip.bind(this) : null }>
                                    <i className="icon icon--lg"></i>
                                </a>
                            </div>
                            <div className="tooltip tooltip--bottom-right" data-open={this.state.IsOpenPhoneTip}>
                                <span className="text">{i18n.lbl_PhoneTooltip}</span>
                            </div>
                        </div>
                    </div>
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

    class ProfilePanel extends React.Component {
        //data-status="error" 1.info, 2.error,3.valid
        constructor(props) {
            super(props);
            this.state = { formArr: null, Profile: null, isUpdate: false };
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
                        this.setState({ Profile: result.data, isUpdate: false });
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
        }

        render() {
            let profile = this.state.Profile;

            return (
                <div>
                    <div className="heading">
                        <span className="text">{i18n.lbl_Profile}</span>
                    </div>
                    {profile != null ? <ProfileForm Profile={profile} getCustInfoProfile={this.getCustInfoProfile.bind(this)} /> : null}
                </div>
            )
        }
    }

    ReactDOM.render(<ProfilePanel />, document.getElementById('Profile'));
})(jQuery, React, ReactDOM, i18n, axios, moment);