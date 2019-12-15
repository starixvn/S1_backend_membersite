import {DataField, InputBox} from '../commJS/InputBox.jsx';

(function ($, React, ReactDOM, i18n, axios) {
    class WireTransfers extends React.Component {    
        constructor(props) {
            super(props);
        }        

        render() {
            return (
            <div class="page">
                <div class="heading">
                    <span class="text">Wire Transfers</span>
                </div>
                <form class="form">
                    <div class="form-group">
                        <label class="form-title">Transfer To</label>
                        <div class="form-content">
                            <div class="dropdown dropdown-default">
                                <a class="btn">
                                    <div class="text-group">
                                        <span class="text">Bank 1</span>
                                        <span class="text">( Limit : <span class="text-accent">200</span> ~ <span class="text-accent">10,000</span> )</span>
                                    </div>
                                    <i class="icon icon-caret"></i>
                                </a>
                                <div class="dropdown__container">
                                    <div class="dropdown__list">
                                        <div class="dropdown__item" data-selected="true">
                                            <div class="text-group">
                                                <span class="text">Bank 1</span>
                                                <span class="text">( Limit : <span class="text-accent">200</span> ~ <span class="text-accent">10,000</span> )</span>
                                            </div>
                                        </div>
                                        <div class="dropdown__item">
                                            <div class="text-group">
                                                <span class="text">Bank 2 Bank 2 Bank 2 Bank 2 Bank 2 Bank 2 Bank 2 Bank 2 Bank 2</span>
                                                <span class="text">( Limit : <span class="text-accent">100</span> ~ <span class="text-accent">5,000</span> )</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="form-group">
                        <label class="form-title">Deposit Amount</label>
                        <div class="form-content" data-status="info">
                            <div class="textfield">
                                <input type="text" class="textfield__input"/>
                                <span class="textfield__bg"></span>
                                <a class="btn btn-icon btn-form-status">
                                    <i class="icon icon--lg"></i>
                                </a>
                            </div>
                            <div class="tooltip tooltip--bottom-right">
                                <span class="text">Tooltip Message</span>
                            </div>
                        </div>
                    </div>
        
                    <div class="btn-group">
                        <button class="btn btn-primary">
                            <span class="text">Back</span>
                        </button>
                        <button class="btn btn-secondary">
                            <div class="btn__inner">
                                <span class="text">Submit</span>
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
                            <a className="payment__item" href="Deposit_WireTransfers.html">
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
        }
   
        render() {
            let panelHolder = <DepositOptions/>;
            return (
                panelHolder
            )
        }
    }
    
    ReactDOM.render(<DepositPanel/>,document.getElementById('DepositPanel'));
    })(jQuery, React, ReactDOM, i18n, axios);