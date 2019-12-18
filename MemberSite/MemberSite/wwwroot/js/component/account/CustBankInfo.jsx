import { DataField, InputBox } from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios) {
    class UpdateCustBankInfo extends React.Component {
        constructor(props) {
            super(props);
            let formArr = [];

            let dataField = new DataField("AccountName");
            dataField.placeholder = "AccountName";
            dataField.displayName = "AccountName";
            dataField.tooltip = "AccountName";
            dataField.handleCheck = this.handleCheckAccountName.bind(this);
            dataField.Value = this.props.Data && this.props.Data.accountName;
            dataField.status = 1;
            if (dataField.Value != null && dataField.Value != "") {
                dataField.status = 4;
            } else {
                dataField.Value = "";
            }

            formArr.push(dataField);

            dataField = new DataField("SysId");
            dataField.displayName = "Bank Name";
            dataField.tooltip = "SysId";
            dataField.status = 1;
            dataField.objectType = 3;
            /*dataField.Value=defCurrencyId;
            dataField.dropdownList=currencyList;*/

            dataField.Value = "";
            formArr.push(dataField);

            dataField = new DataField("AccountNo");
            dataField.placeholder = "AccountNo";
            dataField.displayName = "AccountNo";
            dataField.tooltip = "AccountNo";
            dataField.handleCheck = this.handleCheckAccountNo.bind(this);
            dataField.Value = "";
            dataField.status = 1;
            dataField.objectType = 1;
            formArr.push(dataField);

            this.state = { formArr: formArr, SubmitDisabled: false };
        }

        async handleCheckAccountName(value) {
            console.log(value);
            let status = 1;
            let message = "tooltip";
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = "Need AccountName";
                }
            }

            return { status: status, message: message };
        }

        async handleCheckAccountNo(value) {
            let status = 1;
            let message = "tooltip";
            let regx = /^[0-9]*$/;
            if (value != null) {
                if (value.length == 0) {
                    status = 2;
                    message = "Need AccountNo";
                } else if (!regx.test(value)) {
                    status = 2;
                    message = "0~9";
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
                                        return { text: <b>{bankInfo.bankName}</b>, value: bankInfo.sysId };
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
                <div className="page">
                    <div className="heading">
                        <span className="text">Update Bank Details</span>
                    </div>
                    <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                        {Inputs}
                        <div className="btn-group">
                            {this.props.Data ?
                                <button className="btn btn-primary" onClick={this.onClose.bind(this)}>
                                    <div className="btn__inner">
                                        <span className="text">Back</span>
                                    </div>
                                </button> : null}
                            <button className="btn btn-secondary" type="submit">
                                <div className="btn__inner">
                                    <span className="text">Submit</span>
                                </div>
                            </button>
                        </div>
                    </form>
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
                <div class="page">
                    <div class="heading" role="desktop-only">
                        <span class="text">Bank Info</span>
                    </div>
                    <div class="form-group">
                        <label class="form-title">Bank Account Name</label>
                        <div class="form-content" data-status="readonly">
                            <div class="textfield">
                                <input type="text" class="textfield__input" value={this.props.Data.accountName} readonly />
                                <span class="textfield__bg"></span>
                                <a class="btn btn-icon btn-form-status">
                                    <i class="icon icon--lg"></i>
                                </a>
                            </div>
                            <div class="form-content__tips">
                                <span class="text">For any request of modification, please ask CSD.</span>
                            </div>
                        </div>
                    </div>
                    <div class="btn-group">
                        <button class="btn btn-primary" onClick={bankInfoDetail&&bankInfoDetail.length == 3 ? null : this.onAddCustBankInfo.bind(this)}  data-disabled={bankInfoDetail&&bankInfoDetail.length == 3}>
                            <div className="btn__inner">
                                <span class="text">Add New Account</span>
                            </div>
                        </button>
                    </div>
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
                (this.state.AddCustBankInfo || this.state.Data == null ? <UpdateCustBankInfo Data={this.state.Data} GetCustBankInfo={this.GetCustBankInfo.bind(this)} /> : <CustBankInfoDetail onAddCustBankInfo={this.onAddCustBankInfo.bind(this)} Data={this.state.Data}/>):null
            )
        }
    }

    ReactDOM.render(<CustBankInfo />, document.getElementById('CustBankInfo'));
})(jQuery, React, ReactDOM, i18n, axios);