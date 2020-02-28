import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

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

    class DropdownList extends React.Component {
        constructor(props) {
            super(props);
            this.state = { dropdown: false};
            this.Dropdown = React.createRef();
        }

        onSelect(value){
            this.props.onSelect(value);
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                let dropdownVal = !this.state.dropdown;
                let $target = $(this.Dropdown.current);

                //console.log(typeof($target.attr("data-open")));
                //console.log($target.attr("data-open"));
                if(typeof($target.attr("data-open"))!=dropdownVal) {
                    dropdownVal = true;
                }

                this.setState({dropdown: dropdownVal});
            }
        }

        render() {
            let data = this.props.Data;
            let value = this.props.Value;
            let DesplayText="";
            let DropdownItem= data.map(function( item ) { 
                return <div className="dropdown__item" key={item.Value} onClick={this.onSelect.bind(this, item.Value)} data-selected={item.Value==value}>
                            <div className="text-group">
                                <span className="text">{item.Text}</span>
                            </div>
                        </div>
                }.bind(this));

            if(data!=null) {
                for(let i=0;i<data.length;i++) {
                    if(data[i].Value == value) {
                        DesplayText = data[i].Text;
                        break;
                    }
                }
            }

            return (
                <div className="dropdown dropdown-default" data-open={this.state.dropdown} ref={this.Dropdown}>
                    <a className="btn" onClick={this.onDropdown.bind(this)}>
                        <div className="text-group">
                            <span className="text">{DesplayText}</span>
                        </div>
                        <i className="icon icon-caret"></i>
                    </a>
                    <div className="dropdown__container">
                        {DropdownItem}
                    </div>
                </div>
            )
        }
    }        

    class TransferPanel extends React.Component {
        constructor(props) {
            super(props);
            this.state = { refresh: false, WalletInfo: null, From:0, To:1 ,Amount:1};
        }

        componentDidMount() {
            //this.getWithdrawalAvailableBalance();
            this.getWalletInfo();
        }

        getWalletInfo() {
            this.setState({ refresh: true});
            axios.post('/api/Account/GetWalletInfo')
            .then(
                function(response) {
                    var result = response.data;
                    if (result.errorCode != null && result.errorCode == 0) {
                        this.setState({ refresh: false, WalletInfo: result.data}, function() {
                            
                        });
                    } else {
                        this.setState({ refresh: false});
                    }
                }.bind(this)
              ).catch(function (error) {
                alert(error);
                this.setState({ refresh: false });
            }.bind(this));
        }
        
        onSelectFrom(value){
            let to = this.state.To;

            if(value == 0 && to == 0) {
                let data = this.state.WalletInfo.tpWallets;
                if(data!=null) {
                    for(let i=0;i<data.length;i++) {
                        if(data[i].tpId != 0) {
                            to = data[i].tpId;
                            break;
                        }
                    }
                }

                this.setState({ From: value, To: to });
            } else {
                this.setState({ From: value, To: 0 });
            }
        }

        onSelectTo(value){
            let from = this.state.From;            
            if(value == 0 && from == 0) {
                let data = this.state.WalletInfo.tpWallets;
                if(data != null) {
                    for(let i=0;i<data.length;i++) {
                        if(data[i].tpId != 0) {
                            from = data[i].tpId;
                            break;
                        }
                    }
                }

                this.setState({ To: value, From: from});
            } else {
                this.setState({ To: value, From: 0});
            }
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

        setHighestValue() {
            let WalletInfo = this.state.WalletInfo;
            let from = this.state.From;
            let amount = 0;
            if(WalletInfo!=null) {
                // && WalletInfo.balance > 0
                if(from==0) {
                    amount = WalletInfo.balance;
                } else if(WalletInfo.tpWallets != null) {
                    let data = this.state.WalletInfo.tpWallets;
                    if(data != null) {
                        for(let i=0;i<data.length;i++) {
                            if(data[i].tpId == from) {
                                amount = data[i].balance;
                                break;
                            }
                        }
                    }
                }

                if(IsConvertRequired) {
                    amount/=1000;
                }
                if(amount > 1) {
                    this.setState({Amount:amount});
                } else {
                    this.setState({Amount:1});
                }
            }
            
        }

        handleChange(event) {
            var re = /^\+?[1-9][0-9]*$/;
            if(!isNaN(event.target.value) && re.test(event.target.value)) {
                this.setState({Amount:parseFloat(event.target.value)});
            } else if(event.target.value=="") {
                this.setState({Amount:""});
            }
        }

        async handleSubmit(event) {
            event.preventDefault();
            let Amount = this.state.Amount;
            if(isNaN(Amount) || Amount < 1) {

            } else {
                Loading.OnLoading();
                let from = this.state.From;
                let to = this.state.To;
                let TpDirection = 2;
                let TpId=0;
                if(from == 0) {
                    TpDirection = 1;
                    TpId=to;
                } else {
                    TpId=from;
                }

                if(IsConvertRequired){
                    Amount *=1000;
                }

                let data = {direction: TpDirection, amount:Amount, TpId: TpId};
                axios.post('/api/Tp/Transfer', data)
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        this.getWalletInfo();
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
            }
        }

        onRetrieveAll(){
            Loading.OnLoading();            
            axios.post('/api/Tp/RetrieveAll')
            .then(function (response) {
                Loading.UnLoading();                
                let result = response.data;
                if (result.errorCode == 0) {
                    this.getWalletInfo();
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();                
                alert(error);
            }.bind(this));
        }

        onAllTransferIn(TpId){
            Loading.OnLoading();            
            axios.post('/api/Tp/AllTransferIn',{TpId:TpId})
            .then(function (response) {
                Loading.UnLoading();                
                let result = response.data;
                if (result.errorCode == 0) {
                    this.getWalletInfo();
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();                
                alert(error);
            }.bind(this));
        }     

        onAllTransferOut(TpId){
            Loading.OnLoading();            
            axios.post('/api/Tp/AllTransferOut',{TpId:TpId})
            .then(function (response) {
                Loading.UnLoading();                
                let result = response.data;
                if (result.errorCode == 0) {
                    this.getWalletInfo();
                } else {
                    alert(result.message);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();                
                alert(error);
            }.bind(this));
        }                  

        render() {
            let WalletInfo = this.state.WalletInfo;
            let tpWallets = null;
            let data = [];
            data.push({Text:SiteName,Value:0});
            if(WalletInfo != null && WalletInfo.tpWallets !=null){
                tpWallets = <div className="tbody">{WalletInfo.tpWallets.map(function( item ,index ) { 
                    data.push({Text:item.tpName,Value:item.tpId});
                    return <div className="tr" key={item.tpId}>
                            <div className="td-Secondary-wallet" data-title="Secondary wallet">
                                <span className="text">{item.tpName}</span>
                            </div>
                            <div className="td-Balance" data-title="Balance">
                                <span className="text">{number_format(item.balance)}</span>
                                <a className="btn btn-icon" data-refresh={this.state.refresh} onClick={!this.state.refresh ? this.getWalletInfo.bind(this):null}>
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
                            <div className="td-status">
                                <div className="btn-group">
                                    <button className="btn btn-highlight" type="button" onClick={this.onAllTransferOut.bind(this,item.tpId)}>
                                        <div className="btn__inner">
                                            <span className="text">Transfer Out</span>
                                        </div>
                                    </button>
                                    <button className="btn btn-secondary" type="button" onClick={this.onAllTransferIn.bind(this,item.tpId)}>
                                        <div className="btn__inner">
                                            <span className="text">Transfer To</span>
                                        </div>
                                    </button>
                                </div>
                            </div>
                        </div>;
                }.bind(this))}</div>;
            }

            //console.log(data);

            return (
            <div>
                <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                    <div className="form-group">
                        <label className="form-title">Transfer out of wallet</label>
                        <div className="form-content">
                            <DropdownList Data={data} Value={this.state.From} onSelect={this.onSelectFrom.bind(this)}/>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title">Transfer to wallet</label>
                        <div className="form-content">
                            <DropdownList Data={data} Value={this.state.To} onSelect={this.onSelectTo.bind(this)}/>
                        </div>
                    </div>
                    <div className="form-group">
                        <label className="form-title">Transfer amount</label>
                        <div className="form-content" data-status="valid">
                            <div className="textfield">
                                <input type="number" className="textfield__input" pattern="[0-9]*" value={this.state.Amount} onChange={this.handleChange.bind(this)}/>
                                {IsConvertRequired ? <span className="text">('000)</span> : null}
                                <span className="textfield__bg"></span>
                            </div>
                        </div>
                    </div>
                    <div className="btn-group">
                        <button className="btn btn-secondary dropdown-highlight" type="button" onClick={this.setHighestValue.bind(this)}>
                            <div className="btn__inner">
                                <span className="text">Highest Value</span>
                            </div>
                        </button>
                        <button className="btn btn-secondary" type="submit">
                            <div className="btn__inner">
                                <span className="text">Transfer</span>
                            </div>
                        </button>
                    </div>
                </form>
                <div className="table table-rwd table-primary">
                    <div className="thead" role="desktop-only">
                        <div className="tr">
                            <div className="td-Main-wallet" data-title="Main wallet">
                                <span className="text">Main wallet</span>
                            </div>
                            <div className="td-Balance" data-title="Balance">
                                <span className="text">Balance</span>
                            </div>
                            <div className="td-status" data-title="Retrieve">
                                <span className="text">Retrieve</span>
                            </div>
                        </div>
                    </div>
                    <div className="tbody">
                        <div className="tr">
                            <div className="td-Main-wallet" data-title="Main wallet">
                                <span className="text">{SiteName}</span>
                            </div>
                            <div className="td-Balance" data-title="Balance">
                                <span className="text">{WalletInfo&&number_format(WalletInfo.balance.toFixed(2))}</span>
                                <a className="btn btn-icon" data-refresh={this.state.refresh} onClick={!this.state.refresh ? this.getWalletInfo.bind(this):null}>
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
                            <div className="td-status">
                                <div className="btn-group">
                                    <button className="btn btn btn-primary" onClick={this.onRetrieveAll.bind(this)}>
                                        <div className="btn__inner">
                                            <span className="text">Retrieve all</span>
                                        </div>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div className="table table-rwd table-primary2">
                    <div className="thead" role="desktop-only">
                        <div className="tr">
                            <div className="td-Secondary-wallet" data-title="Secondary wallet">
                                <span className="text">Secondary wallet</span>
                            </div>
                            <div className="td-Balance" data-title="Balance">
                                <span className="text">Balance</span>
                            </div>
                            <div className="td-status" data-title="Transfer all">
                                <span className="text">Transfer all</span>
                            </div>
                        </div>
                    </div>
                    {tpWallets}
                </div>
            </div>
            )
        }
    }
    ReactDOM.render(<TransferPanel/>, document.getElementById('TransferPanel'));    
})(jQuery, React, ReactDOM, i18n, axios, moment);