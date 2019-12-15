import {DataField, InputBox} from '../commJS/InputBox.jsx';
import * as Loading from '../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios) {
    class BankInfoOffDropdown extends React.Component {
        constructor(props) {
            super(props);
            this.state = { dropdown: false};
        }        
        onPickBankInfoOff(PickBankInfoOff){
            this.props.onPickBankInfoOff(PickBankInfoOff);
            this.setState({dropdown:false});
        }
        
        onDropdown(event) {
            let node = event.target;
            let parentNode = node.parentNode;
            if(node.className != "btn btn-icon btn-form-status" && (parentNode!=undefined && parentNode.className != "btn btn-icon btn-form-status")) {
                this.setState({dropdown:!this.state.dropdown}, function() {
                    if(this.state.dropdown){
                        let object = document.getElementsByTagName('body')[0];
                        $(object).on("mousedown", this.onHideDropdown.bind(this));
                    } else {
                        let object = document.getElementsByTagName('body')[0];
                        $(object).off();                
                    }
                });
            }
        }

        onHideDropdown(event) {
            let node = event.target.parentNode;
            let i=1;
            while (node != undefined && i<=3) {
                if(node.className == "dropdown dropdown-default"){
                    return;
                }
    
                node = node.parentNode;
                i++;
            }
    
            this.setState({dropdown:false}, function() {
                let object = document.getElementsByTagName('body')[0];
                $(object).off();
            });
        }

        render() {
            let DropdownItem=this.props.BankInfoOffList.map(function( data ) { 
                return <div className="dropdown__item" key={data.sysId} onClick={this.onPickBankInfoOff.bind(this, data)}>
                    <div className="text-group">
                        <span className="text">{data.bankName}</span>
                        {data.minAmount == 0 && data.maxAmount == 0 ? 
                            <span className="text">{"( Limit : "}<span className="text-accent">{"unlimited."}</span></span>
                        : <span className="text">{"( Limit : "}<span className="text-accent">{data.minAmount}</span>{" ~ "}<span className="text-accent">{data.maxAmount}</span>{" )"}</span>
                        }
                    </div>
                </div>                
                }.bind(this));

            let PickBankInfoOff = this.props.PickBankInfoOff;
            return (
                <div className="form-group">
                    <label className="form-title">Transfer To</label>
                    <div className="form-content">
                        <div className="dropdown dropdown-default" data-open={this.state.dropdown}>
                            <a className="btn" onClick={this.onDropdown.bind(this)}>
                                <div className="text-group">
                                    <span className="text">{PickBankInfoOff.bankName}</span>
                                    {PickBankInfoOff.minAmount == 0 && PickBankInfoOff.maxAmount == 0 ? 
                                        <span className="text">{"( Limit : "}<span className="text-accent">{"unlimited."}</span></span>
                                    : <span className="text">{"( Limit : "}<span className="text-accent">{PickBankInfoOff.minAmount}</span>{" ~ "}<span className="text-accent">{PickBankInfoOff.maxAmount}</span>{" )"}</span>
                                    }
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
    
    class WireTransfers extends React.Component {    
        constructor(props) {
            super(props);

            let formArr=[];

            let amountDataField = new DataField("Amount");
            amountDataField.placeholder="Amount";
            amountDataField.displayName="Amount";
            amountDataField.tooltip="Amount";
            amountDataField.status=1;
            amountDataField.objectType=5;
            amountDataField.handleCheck = this.handleCheckAmount.bind(this);
            amountDataField.Value="";
            formArr.push(amountDataField);

            this.state = { formArr: formArr, BankInfoOffList: null, PickBankInfoOff:null};
        }        

        componentDidMount() {
            Loading.OnLoading();            
            axios.post('/api/Payment/GetBankInfoOffList')
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    let BankInfoOff=null;
                    if(result.data.length>0) {
                        BankInfoOff = result.data[0];
                    }

                    this.setState({BankInfoOffList: result.data, PickBankInfoOff: BankInfoOff});
                } else {
                    alert(result.message);
                    this.onDepositOptions();
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
                this.onDepositOptions();
            }.bind(this));
        }

        onPickBankInfoOff(BankInfoOff){
            this.setState({PickBankInfoOff: BankInfoOff});
        }

        async handleCheckAmount(value) {
            let status = 1;
            let message = "";
            let PickBankInfoOff = this.state.PickBankInfoOff;
            if(value!=null) {
                if(value <= 0) {
                    status = 2;
                    message = "Can't Be 0.";
                } else if((PickBankInfoOff.minAmount != 0 && PickBankInfoOff.maxAmount != 0) && (value > PickBankInfoOff.maxAmount || value < PickBankInfoOff.minAmount) ) {
                    status = 2;
                    message = value + " not in Limit.";
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
                this.forceUpdate();
            } else {
                this.forceUpdate();
            }
        } 
        
        onDepositOptions() {
            this.props.onDepositOptions();
        }

        render() {
            let Inputs=this.state.formArr.map(dataField => <InputBox key={dataField.name} DataField={dataField}/>);
            let BankInfoOffList = this.state.BankInfoOffList;
            if(BankInfoOffList != null && BankInfoOffList.length > 0) {
                Inputs.push(<BankInfoOffDropdown key={"."} BankInfoOffList={this.state.BankInfoOffList} PickBankInfoOff={this.state.PickBankInfoOff} onPickBankInfoOff={this.onPickBankInfoOff.bind(this)} />);
            }

            return (
            <div className="page">
                <div className="heading">
                    <span className="text">Wire Transfers</span>
                </div>
                <form className="form" onSubmit={this.handleSubmit.bind(this)}>
                    {Inputs}
                    <div className="btn-group">
                        <button className="btn btn-primary" type="button" onClick={this.onDepositOptions.bind(this)}>
                            <span className="text">Back</span>
                        </button>
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

    class DepositOptions extends React.Component {
        constructor(props) {
            super(props);
        }

        onWireTransfers() {
            this.props.onWireTransfers();
        }
   
        render() {
    
            return (
            <div className="page">
                <div className="heading" role="desktop-only">
                    <span className="text">Deposit Options</span>
                </div>
                <p>You can choose the best suitable for you deposit option from the ones provided below:.</p>
                <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">Wire Transfers -</span>
                        <span className="text-primary">Credit Time : 10 Mins</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment">
                            <a className="payment__item" href="javascript:void(0);" onClick={this.onWireTransfers.bind(this)}>
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/wire-transfer.png"/>
                                </div>
                                <span className="payment__text">Wire Transfers</span>
                            </a>
                        </div>
                    </div>
                </div>
                <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">Online Banking -</span>
                        <span className="text-primary">Credit Time : Instant</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment">
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/hong-leong-bank.png"/>
                                </div>
                                <span className="payment__text">Hong Leong</span>
                            </a>
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/cimb.png"/>
                                </div>
                                <span className="payment__text">CIMB</span>
                            </a>
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/help2pay.png"/>
                                </div>
                                <span className="payment__text">Help 2 Pay</span>
                            </a>
                        </div>
                    </div>
                </div>
                <div className="panel panel-primary panel--payment" data-open="true">
                    <div className="panel__header">
                        <span className="text">Mobile Deposit -</span>
                        <span className="text-primary">Credit Time : Instant</span>
                        <i className="icon icon-caret"></i>
                    </div>
                    <div className="panel__body">
                        <div className="payment">
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/alipay.png"/>
                                </div>
                                <span className="payment__text">Alipay</span>
                            </a>
                            <a className="payment__item" href="#">
                                <div className="payment__img">
                                    <img src="../../SiteSkin/Nova88/common/images/payment/wechat-pay.png"/>
                                </div>
                                <span className="payment__text">WeChat Pay</span>
                            </a>
                        </div>
                    </div>
                </div>                
            </div>                
            )
        }
    }

    class DepositPanel extends React.Component {
        constructor(props) {
            super(props);
            this.state={"DipositType": 0};
        }

        onWireTransfers() {
            this.setState({"DipositType": 1});
        }

        onDepositOptions() {
            this.setState({"DipositType": 0});
        }
   
        render() {
            let panelHolder = <DepositOptions onWireTransfers={this.onWireTransfers.bind(this)}/>;
            if(this.state.DipositType == 1) {
                panelHolder = <WireTransfers onDepositOptions={this.onDepositOptions.bind(this)}/>;
            } 

            return (
                panelHolder
            )
        }
    }
    
    ReactDOM.render(<DepositPanel/>,document.getElementById('DepositPanel'));
    })(jQuery, React, ReactDOM, i18n, axios);