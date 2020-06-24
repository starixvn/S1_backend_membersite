import {DataField, InputBox} from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';
import {ShowModal} from '../commJS/Modal.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class RecentHistoryBody extends React.Component {
        onCancel(transId){
            if(confirm(i18n.lbl_CancelConfirm)) {
                this.props.onCancel(transId);
            }
        }

        render() {
            let data = this.props.Data;
            let rows = data.map(function( item ,index ) { 
                let dateMonent = moment(item.createTime);
                let btnGroup = null;
                let statusGroup = null;

                if(item.status==1) {
                    btnGroup = <div className="btn-group">
                        <button className="btn btn-default" type="button" onClick={this.onCancel.bind(this,item.transId)}>
                            <span className="text">{i18n.lbl_Cancel}</span>
                        </button>
                    </div>
                }

                if (item.status==5 || item.status==6 || item.status==7 || item.status==99) {
                    statusGroup = <span className="text">{i18n["lbl_WithdrawalStatus"+item.status]}</span>
                }
                else {
                    statusGroup = <span className="text">{i18n.lbl_WithdrawalStatus4}</span>
                }

                return <div className="tr" key={index}>
                    <div className="td-date" data-title="Date" role="mobile-click">
                        <span className="text">{dateMonent.format('l')} ({dateMonent.format('Z')})</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="td-time" data-title="Time">
                        <span className="text">{dateMonent.format("hh:mm:ss A")}</span>
                    </div>
                    <div className="td-id" data-title="ID">
                        <span className="text">{item.transId}</span>
                    </div>
                    <div className="td-amount" data-title="Amount">
                        <span className="text">{number_format(item.amount.toFixed(2))}</span>
                    </div>
                    <div className="td-status" data-title="Status">
                        {statusGroup}
                        {btnGroup}
                    </div>
            </div>;
                }.bind(this));

                let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
            return (
                <div className="tbody" data-date={dateStr} data-open="true">
                    {rows}
                </div>
            )
        }
    }

    class RecentHistory extends React.Component {
        constructor(props) {
            super(props);
        }

        componentDidMount() {
            this.GetWithdrawalInfoRecent();
        }

        GetWithdrawalInfoRecent(){
            this.props.GetWithdrawalInfoRecent();
        }

        render() {
            let data=this.props.Data;

            let bodyData = null;
            if(data!=null){
                let dataContainer=[];
                for(let i=0;i < data.length;i++){
                    let row = data[i];
                    let mDate = moment(row.createTime);
                    let sDate = mDate.format("YYYYMMDD");
                    if(dataContainer.length==0 || dataContainer[dataContainer.length-1].Date != sDate) {
                        dataContainer.push({Date:sDate, Data:[row]});
                    } else {
                        dataContainer[dataContainer.length-1].Data.push(row);
                    }
                }

                bodyData = dataContainer.map(function( item ,index ) { 
                    return <RecentHistoryBody key={index} Data={item.Data} onCancel={this.props.onCancel}/>;
                }.bind(this));
            }

            return (
                <div className="table table-rwd table-primary">
                    <div className="caption">
                        <span className="text">{i18n.lbl_RecentTransactions}</span>
                        <a className="btn btn-icon" data-refresh={this.props.refresh} onClick={this.props.refresh?null:this.GetWithdrawalInfoRecent.bind(this)}>
                            <i className="icon icon-refresh"></i>
                            <div className="preloader">
                                <div className="preloader__spiner">
                                    <div className="preloader__circles">
                                        <div className="preloader__circle-1"></div>
                                        <div className="preloader__circle-2"></div>
                                    </div>
                                </div>
                            </div>
                        </a>
                    </div>
                    <div className="thead" role="desktop-only">
                        <div className="tr">
                            <div className="td-date" data-title="Date">
                                <span className=" text">{i18n.lbl_Date}</span>
                            </div>
                            <div className="td-time" data-title="Time">
                                <span className=" text">{i18n.lbl_Time}</span>
                            </div>
                            <div className="td-id" data-title="ID">
                                <span className=" text">{i18n.lbl_ID}</span>
                            </div>
                            <div className="td-amount" data-title="Amount">
                                <span className=" text">{i18n.lbl_Amount}</span>
                            </div>
                            <div className="td-status" data-title="Status">
                                <span className=" text">{i18n.lbl_Status}</span>
                            </div>
                        </div>
                    </div>
                    {bodyData}
                    <div className="tfoot">
                        <div className="tr">
                            <div className="td-right">
                                <a className="text-link" href={hrefAllTrans}>
                                    <span className="text">{i18n.lbl_AllTranscations}</span>
                                    <i className="icon icon-arrow-right"></i>
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            )
        }
    }

    class CustBankInfoDropdown extends React.Component {
        constructor(props) {
            super(props);
            this.state = { dropdown: false};
            this.Dropdown = React.createRef();
        }        
        onPickBankInfo(bankInfo){
            this.props.onPickBankInfo(bankInfo);
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                let dropdownVal = !this.state.dropdown;
                let $target = $(this.Dropdown.current);

                if(typeof($target.attr("data-open"))=="undefined") {
                    $target.attr("data-open",true);
                    dropdownVal = true;
                }

                this.setState({dropdown: dropdownVal});
            }
        }

        render() {
            let PickBankInfo = this.props.PickBankInfo;            
            let DropdownItem=this.props.BankInfoDetail.map(function( data, index ) { 
                return <div className="dropdown__item" key={index} onClick={this.onPickBankInfo.bind(this, data)} data-selected={data.accountNo==PickBankInfo.accountNo}>
                    <div className="payment-thumbnail">
                        <img src={"/common/images/BankLogo/" + data.bankLogo} />
                    </div> 
                    <div className="text-group">
                        <span className="text">{data.bankName}</span>
                        <span className="text">{data.accountNo}</span>
                    </div>
                </div>                
                }.bind(this));

            return (
                <div className="form-group">
                    <label className="form-title">{i18n.lbl_TransferTo}</label>
                    <div className="form-content">
                        <div className="dropdown dropdown-default" data-open={this.state.dropdown} ref={this.Dropdown}>
                            <a className="btn" onClick={this.onDropdown.bind(this)}>
                                <div className="payment-thumbnail">
                                    <img src={"/common/images/BankLogo/" + PickBankInfo.bankLogo} />
                                </div> 
                                <div className="text-group">
                                    <span className="text">{PickBankInfo.bankName}</span>
                                    <span className="text">{PickBankInfo.accountNo}</span>
                                </div>
                                <i className="icon icon-caret"></i>
                            </a>
                            <div className="dropdown__container">
                                {DropdownItem}
                            </div>
                        </div>
                    </div>
                </div>
            )
        }
    }

    class TbdModal extends React.Component {
        constructor(props) {
            super(props);
            this.state={Show: false};
        }

        componentDidMount() {
            this.setState({Show: true});
        }

        onClose(){
            this.props.onClose();
        }

        onConfirm(event){
            event.preventDefault();
            this.props.onConfirm();
        }

        setMemo(event) {
            this.props.setMemo(event.target.value);
        }

        render() {
            return (
            <div className="modal" data-open={this.state.Show}>
                <div className="overlay" onClick={this.onClose.bind(this)}></div>
                <div className="modal__dialog">
                    <div className="modal__header">
                        <span className="text">{i18n.lbl_ImportantNote}</span>
                    </div>
                    <form className="modal__body" onSubmit={this.onConfirm.bind(this)}>
                        <p>{i18n.lbl_lbl_ConfirmWithdrawal}</p>
                        <div className="table table-modal">
                            <div className="tbody">
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_WithdrawalTo}</span>
                                    </div>
                                    <div className="td-right">
                                        <span className="text">{this.props.PickBankInfo.bankName}</span>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankAccountName}</span>
                                    </div>
                                    <div className="td-right">
                                        <span className="text">{this.props.AcountName}</span>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankNumber}</span>
                                    </div>
                                    <div className="td-right">
                                        <span className="text">{this.props.PickBankInfo.accountNo}</span>
                                    </div>
                                </div>                                
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_WithdrawalAmount}</span>
                                    </div>
                                    <div className="td-right">
                                        <span className="text">{number_format(parseFloat(this.props.Amount).toFixed(2))}</span>
                                    </div>
                                </div>
                            </div>
                        </div>                     
                        <div className="btn-group">
                            <button className="btn btn-primary" onClick={this.onClose.bind(this)} type="button">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Cancel}</span>
                                </div>
                            </button>
                            <button className="btn btn-secondary" type="submit">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Confirm}</span>
                                </div>
                            </button>
                        </div>
                    </form>
                </div>
            </div>
            )
        }
    }    

    class WithdrawalPanel extends React.Component {
        constructor(props) {
            super(props);

            let formArr=[];

            let amountDataField = new DataField("Amount");
            amountDataField.placeholder = i18n.lbl_WithdrawalAmount;
            amountDataField.displayName = i18n.lbl_WithdrawalAmount;
            amountDataField.tooltip = i18n.lbl_AmountTooltip;
            amountDataField.status = 1;
            amountDataField.objectType = 5;
            amountDataField.handleCheck = this.handleCheckAmount.bind(this);
            amountDataField.Value = "";
            formArr.push(amountDataField);

            this.state = { formArr: formArr, showTodModal: false,refresh:false};
        }

        componentDidMount() {
            this.getWithdrawalAvailableBalance();
            this.getCustBankInfo();
        }

        getCustBankInfo() {
            Loading.OnLoading();            
            axios.post('/api/Account/GetCustBankInfo')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0 && result.data.bankInfoDetail.length > 0) {
                    this.setState({BankInfoDetail: result.data.bankInfoDetail, AcountName: result.data.accountName, PickBankInfo: result.data.bankInfoDetail[0]});
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            }.bind(this));
        }

        getWithdrawalAvailableBalance() {
            Loading.OnLoading();            
            axios.post('/api/Account/GetWithdrawalAvailableBalance')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({
                        Balance: result.data.balance, 
                        Deduction: result.data.deduction, 
                        AvailableBalance: result.data.availableBalance});
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            }.bind(this));
        }        

        async onPickBankInfo(BankInfo){
            this.setState({PickBankInfo: BankInfo},async function(){
                for(var i=0; i < this.state.formArr.length; i++){
                    let dataField = this.state.formArr[i];
                    await dataField.onCheck();
                }

                this.forceUpdate();
            }.bind(this));
        }

        async handleCheckAmount(value) {
            let status = 1;
            let message = "";
            
            if(isNaN(value)) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            } else if(value <= 0) {
                status = 2;
                message = i18n.lbl_AmountTooltip;
            /*} else if((PickBankInfoOff.minAmount != 0 && PickBankInfoOff.maxAmount != 0) && (value > PickBankInfoOff.maxAmount || value < PickBankInfoOff.minAmount) ) {
                status = 2;
                message = number_format(value) + " not in Limit.";*/
            } else {
                status = 3;
                message = "";
            }
    
            return {status: status, message: message};
        }

        onCloseModal() {
            this.setState({showModal:false});
        }

        onClose() {
            this.setState({showTodModal:false});
        }
        
        setMemo(value) {
            this.setState({Memo: value});
        }

        onConfirm() {
            this.setState({showTodModal:false});
            Loading.OnLoading();
            let data = {sysId: this.state.PickBankInfo.sysId, AccountNo: this.state.PickBankInfo.accountNo, amount:this.state.Amount, memo:this.state.Memo};
            axios.post('/api/Payment/WithdrawalReq', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    //ShowModal
                    alert(result.message);
                    this.setState({showTodModal:false}, function(){
                        this.GetWithdrawalInfoRecent();
                        this.getWithdrawalAvailableBalance();
                    }.bind(this));
                } else if (result.errorCode == 2) {
                    window.location.replace(window.location.href);
                } else if (result.errorCode < 0) {
                    if(result.errorCode==-1) {
                        ShowModal(i18n.lbl_ImportantNote, i18n.lbl_RolloverOver + ": " + number_format(parseFloat(result.message)));
                    } else if(result.errorCode==-2) {
                        ShowModal(i18n.lbl_ImportantNote, i18n.lbl_DepositOver + ": " + number_format(parseFloat(result.message)));
                    } else if(result.errorCode==-3) {
                        ShowModal(i18n.lbl_ImportantNote, i18n.lbl_WithdrawalOver + ": " + number_format(parseFloat(result.message)));                        
                    }

                    this.setState({showTodModal:true});
                } else {
                    alert(result.message);
                    this.setState({showTodModal:true});
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.setState({showTodModal:true});                    
            }.bind(this));
        }

        onCancel(transId) {
            Loading.OnLoading();
            axios.post('/api/Payment/SetWithdrawalCancel', {TransId: transId})
            .then(function (response) {
                Loading.UnLoading();                
                let result = response.data;
                if (result.errorCode == 0) {
                    this.GetWithdrawalInfoRecent();
                    this.getWithdrawalAvailableBalance();
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            }.bind(this));
        }

        GetWithdrawalInfoRecent(){
            this.setState({refresh: true});
            axios.post('/api/Payment/GetWithdrawalInfoRecent')
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {
                    this.setState({WithdrawalInfoRecent: result.data, refresh: false});
                } else {
                    this.setState({refresh: false});
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                this.setState({refresh: false});
                alert(error);
            }.bind(this));
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
                this.setState({showTodModal: true, Amount: parseFloat(data["Amount"])});
            }
        }
   
        render() {
            let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
            let bankInfoDetail = this.state.BankInfoDetail;
            if(bankInfoDetail != null && bankInfoDetail.length > 0) {
                Inputs.push(<CustBankInfoDropdown key={"bankInfoDetail"} BankInfoDetail={bankInfoDetail} PickBankInfo={this.state.PickBankInfo} onPickBankInfo={this.onPickBankInfo.bind(this)} />);
            }

            let modalContaner=null;
            let _balance = this.state.Balance != null || this.state.Balance != undefined ? this.state.Balance : 0;
            let _deduction = this.state.Deduction != null || this.state.Deduction != undefined ? this.state.Deduction : 0;
            let _availableBalance = this.state.AvailableBalance != null || this.state.AvailableBalance != undefined ? this.state.AvailableBalance : 0;

            return this.state.Balance === undefined || this.state.Deduction === undefined || this.state.AvailableBalance === undefined ? <div /> : (
                    <div>
                        <div className="heading">
                            <span className="text">{i18n.lbl_Withdrawal}</span>
                        </div>
                        <div className="form-group WAB">
                            <div className="Calculation">
                                {
                                    _deduction <= 0 ? null : (
                                        <>
                                            <div className="row">
                                                <div className="form-title">{i18n.lbl_Balance}：{i18n.lbl_Currency}</div>
                                                <div className="form-content form-content--text">
                                                    <span className="text">{number_format(_balance)}</span>
                                                </div>
                                            </div>
                                            <div className="row">
                                                <div className="form-title">{i18n.lbl_Deduction}：{i18n.lbl_Currency}</div>
                                                <div className="form-content form-content--text">
                                                    <span className="text">- {number_format(Math.abs(_deduction))}</span>
                                                </div>
                                            </div>
                                            <div className="row symbol">=</div>
                                        </>
                                    )
                                }
                                <div className="row">
                                    <div className="form-title">{i18n.lbl_EligibleWithdrawalAmount}：{i18n.lbl_Currency}</div>
                                    <div className="form-content form-content--text">
                                        <span className="text">{number_format(_availableBalance)}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                            <div className="form-group">
                                <label className="form-title">{i18n.lbl_BankAccountName}</label>
                                <div className="form-content form-content--text">
                                        <span className="text">{this.state.AcountName}</span>
                                </div>
                            </div>
                            {Inputs}
                            <div className="btn-group">
                                <button className="btn btn-secondary" type="submit">
                                    <div className="btn__inner">
                                        <span className="text">{i18n.lbl_Submit}</span>
                                    </div>
                                </button>
                            </div>
                        </form>
                        {this.state.showTodModal ? <TbdModal setMemo={this.setMemo.bind(this)} onConfirm={this.onConfirm.bind(this)} onClose={this.onClose.bind(this)} PickBankInfo={this.state.PickBankInfo} Amount={this.state.Amount} Memo={this.state.Memo} AcountName={this.state.AcountName} /> : null}                        
                        <RecentHistory GetWithdrawalInfoRecent={this.GetWithdrawalInfoRecent.bind(this)} onCancel={this.onCancel.bind(this)} Data={this.state.WithdrawalInfoRecent} refresh={this.state.refresh}/>
                        {modalContaner}
                    </div>
            )
        }
    }
    ReactDOM.render(<WithdrawalPanel/>, document.getElementById('WithdrawalPanel'));    
})(jQuery, React, ReactDOM, i18n, axios, moment);