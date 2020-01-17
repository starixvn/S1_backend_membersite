import { DataField, InputBox } from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios) {
    class UpdateCustBankInfo extends React.Component {
        constructor(props) {
            super(props);
            let formArr = [];

            let dataField = new DataField("AccountName");
            dataField.placeholder = i18n.lbl_BankAccountName;
            dataField.displayName = i18n.lbl_BankAccountName;
            dataField.Value = this.props.Data && this.props.Data.accountName;
            dataField.status = 1;
            if (dataField.Value != null && dataField.Value != "") {
                dataField.status = 4;
            } else {
                dataField.Value = "";
                dataField.tooltip = i18n.lbl_BankAccountNameHint;                
                dataField.handleCheck = this.handleCheckAccountName.bind(this);
            }

            formArr.push(dataField);

            dataField = new DataField("SysId");
            dataField.displayName = i18n.lbl_BankName;
            dataField.tooltip = i18n.lbl_BankName;
            dataField.status = 1;
            dataField.objectType = 3;
            /*dataField.Value=defCurrencyId;
            dataField.dropdownList=currencyList;*/

            dataField.Value = "";
            formArr.push(dataField);

            dataField = new DataField("AccountNo");
            dataField.placeholder = i18n.lbl_BankNumber;
            dataField.displayName = i18n.lbl_BankNumber;
            dataField.tooltip = i18n.lbl_BankNumber;
            dataField.handleCheck = this.handleCheckAccountNo.bind(this);
            dataField.Value = "";
            dataField.status = 1;
            dataField.objectType = 1;
            formArr.push(dataField);

            this.state = { formArr: formArr, SubmitDisabled: false };
        }

        async handleCheckAccountName(value) {
            let status = 1;
            let message = i18n.msg_NeedAccountName;
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = i18n.msg_NeedAccountName;
                } else {
                    status = 3;
                    message = "";
                }
            }

            return { status: status, message: message };
        }

        async handleCheckAccountNo(value) {
            let status = 1;
            let message = i18n.msg_NeedAccountNo;
            let regx = /^[0-9]*$/;
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = i18n.msg_NeedAccountNo;
                } else if (!regx.test(value)) {
                    status = 2;
                    message = "0~9";
                } else {
                    status = 3;
                    message = "";
                }
            }

            return { status: status, message: message };
        }

        componentDidMount() {
            Loading.OnLoading();
            axios.post('/api/Payment/GetBankInfoSysList')
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        if (result.data && result.data.length > 0) {
                            let PickBankInfo = result.data[0];

                            for (let i = 0; i < this.state.formArr.length; i++) {
                                let dataField = this.state.formArr[i];

                                if (dataField.name == "SysId") {
                                    let dropdownList = result.data.map(function (bankInfo) {
                                        return { text: <div className="dropdown__item">
                                                <div className="payment-thumbnail">
                                                    <img src={"/common/images/BankLogo/" + bankInfo.bankLogo} />
                                                </div>
                                                <div className="text-group">
                                                    <span className="text">{bankInfo.bankName}</span>
                                                </div>
                                            </div>, value: bankInfo.sysId };
                                    });

                                    dataField.dropdownList = dropdownList;
                                    dataField.Value = PickBankInfo.sysId;

                                    break;
                                }
                            }
                        }

                        this.forceUpdate();
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
        }

        async handleSubmit(event) {
            event.preventDefault();
            Loading.OnLoading();
            let data = {};
            let valid = true;
            for (var i = 0; i < this.state.formArr.length; i++) {
                let dataField = this.state.formArr[i];
                await dataField.onCheck();
                if (valid) {
                    if (dataField.status == 2) {
                        valid = false;
                    }
                }
                data[dataField.name] = dataField.Value;
            }

            if (!valid) {
                Loading.UnLoading();
                this.setState({ SubmitDisabled: true });
            } else {
                this.setState({ SubmitDisabled: false });
                axios.post('/api/AccountStrict/SetCustBankInfo', data)
                    .then(function (response) {
                        Loading.UnLoading();
                        let result = response.data;
                        if (result.errorCode == 0) {
                            alert(result.message);
                            this.onClose();
                        } else {
                            alert(result.message);
                        }
                    }.bind(this)).catch(function (error) {
                        Loading.UnLoading();
                        alert(error);
                    });
            }
        }

        onClose() {
            this.props.GetCustBankInfo();
        }

        render() {
            let Inputs = this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField} />);

            return (
                <div>
                    <div className="heading">
                        <span className="text">{i18n.lbl_CustBankInfo}</span>
                    </div>
                    <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                        {Inputs}
                        <div className="btn-group">
                            {this.props.Data ?
                                <button className="btn btn-primary" type="button" onClick={this.onClose.bind(this)}>
                                    <div className="btn__inner">
                                        <span className="text">{i18n.lbl_Back}</span>
                                    </div>
                                </button> : null}
                            <button className="btn btn-secondary" type="submit" data-disabled={this.state.SubmitDisabled}>
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Submit}</span>
                                </div>
                            </button>
                        </div>
                    </form>
                </div>
            )
        }
    }
    class BankAccountList extends React.Component {
        constructor(props) {
            super(props);
            //this.state = { };
        }

        DelCustBankInfo(bankInfo) {
            if (confirm(i18n.lbl_CheckConfirm)) {
                Loading.OnLoading();                
                axios.post('/api/AccountStrict/DelCustBankInfo', bankInfo)
                    .then(function (response) {
                        let result = response.data;
                        if (result.errorCode == 0) {
                            alert(result.message);
                            this.props.GetCustBankInfo();
                        } else {
                            alert(result.message);
                        }
                        Loading.UnLoading();
                    }.bind(this)).catch(function (error) {
                        Loading.UnLoading();
                        alert(error);
                    }.bind(this));
            }
        }

        render() {
            let rows = this.props.bankInfoDetail.map(function (bankInfo) {
                return <div key={bankInfo.accountNo} className="tbody">
                    <div className="tr" role="mobile-grid">
                        <div className="td-bank-img" role="mobile-rowspan">
                            <div className="payment-thumbnail">
                                <img src={"/common/images/BankLogo/" + bankInfo.bankLogo} />
                            </div>
                        </div>
                        <div className="td-bank-name" role="mobile-row">
                            <span className="text">{bankInfo.bankName}</span>
                        </div>
                        <div className="td-bank-number" role="mobile-row">
                            <span className="text">{bankInfo.accountNo}</span>
                        </div>
                        <div className="td-icon" role="mobile-rowspan">
                            <a className="btn btn-icon text-primary" onClick={this.DelCustBankInfo.bind(this, bankInfo)}>
                                <i className="icon icon-delete-outline"></i>
                            </a>
                        </div>
                    </div>
                </div>;
            }.bind(this));

            return (
                <div className="table table-rwd table-primary">
                    <div className="caption">
                        <span className="text">{i18n.lbl_BankAccount}</span>
                    </div>
                    <div className="thead" role="desktop-only">
                        <div className="tr">
                            <div className="td-bank-img"></div>
                            <div className="td-bank-name" data-title={i18n.lbl_BankName}>
                                <span className="text">{i18n.lbl_BankName}</span>
                            </div>
                            <div className="td-bank-number" data-title={i18n.lbl_BankNumber}>
                                <span className="text">{i18n.lbl_BankNumber}</span>
                            </div>
                            <div className="td-icon"></div>
                        </div>
                    </div>
                        {rows}
                    </div>
            )
        }
    }

    class CustBankInfoDetail extends React.Component {
        constructor(props) {
            super(props);
            this.state = { };
        }

        onAddCustBankInfo(){
            this.props.onAddCustBankInfo();
        }

        render() {
            let bankInfoDetail = this.props.Data.bankInfoDetail;

            return (
                <div>
                    <div className="heading">
                        <span className="text">{i18n.lbl_BankInfo}</span>
                    </div>
                    <form className="form">
                        <div className="form-group">
                            <label className="form-title">{i18n.lbl_BankAccountName}</label>
                            <div className="form-content" data-status="readonly">
                                <div className="textfield">
                                    <input type="text" className="textfield__input" value={this.props.Data.accountName} readOnly />
                                    <span className="textfield__bg"></span>
                                    <a className="btn btn-icon btn-form-status">
                                        <i className="icon icon--lg"></i>
                                    </a>
                                </div>
                                <div className="form-content__tips">
                                    <span className="text">{i18n.lbl_ContactCSD}</span>
                                </div>
                            </div>
                        </div>
                    </form>
                    <div className="btn-group">
                        <button className="btn btn-primary" onClick={bankInfoDetail&&bankInfoDetail.length == 3 ? null : this.onAddCustBankInfo.bind(this)}  data-disabled={bankInfoDetail&&bankInfoDetail.length == 3}>
                            <div className="btn__inner">
                                <span className="text">{i18n.lbl_AddNewAccount}</span>
                            </div>
                        </button>
                    </div>
                    {bankInfoDetail&&bankInfoDetail.length>0 ? <BankAccountList bankInfoDetail={bankInfoDetail} GetCustBankInfo={this.props.GetCustBankInfo}/> : null}
                </div>
            )
        }
    }

    class CustBankInfo extends React.Component {
        constructor(props) {
            super(props);
            this.state = { AddCustBankInfo: false, GetCustBankInfo: false};
        }

        componentDidMount() {
            this.GetCustBankInfo();
        }

        GetCustBankInfo() {
            Loading.OnLoading();
            axios.post('/api/Account/GetCustBankInfo')
                .then(function (response) {
                    let result = response.data;
                    if (result.errorCode == 0) {
                        this.setState({ Data: result.data, AddCustBankInfo: false, GetCustBankInfo: true });
                    } else {
                        alert(result.message);
                    }
                    Loading.UnLoading();
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
        }

        onAddCustBankInfo() {
            this.setState({ AddCustBankInfo: true });
        }

        render() {
            return (
                this.state.GetCustBankInfo ? 
                (this.state.AddCustBankInfo || this.state.Data == null ? <UpdateCustBankInfo Data={this.state.Data} GetCustBankInfo={this.GetCustBankInfo.bind(this)} /> : <CustBankInfoDetail  GetCustBankInfo={this.GetCustBankInfo.bind(this)} onAddCustBankInfo={this.onAddCustBankInfo.bind(this)} Data={this.state.Data}/>):null
            )
        }
    }

    ReactDOM.render(<CustBankInfo />, document.getElementById('CustBankInfo'));
})(jQuery, React, ReactDOM, i18n, axios);