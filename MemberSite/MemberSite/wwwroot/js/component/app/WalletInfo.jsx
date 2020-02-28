import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment) {

    class WalletInfo extends React.Component {
        //data-status="error" 1.info, 2.error,3.valid
        constructor(props) {
            super(props);
            this.state = { refresh: false, WalletInfo: null};
        }
    
        getWalletInfo() {
            this.setState({ refresh: true});
            axios.post('/api/Account/GetWalletInfo')
            .then(
                function(response) {
                    var result = response.data;
                    if (result.errorCode != null && result.errorCode == 0) {
                        this.setState({ refresh: false, WalletInfo: result.data}, function() {
                            let balance = i18n.lbl_Currency;
                            if(!result.data.isHideBalance){
                                balance = balance + ' ' +number_format(result.data.balance.toFixed(2));
                            }
                            $(".WalletInfo").html(balance);
                        });
                    } else {
                        this.setState({ refresh: false});
                    }
                }.bind(this)
              ).catch(function (error) {
                alert(error);
                this.setState({ refresh: false});      
            }.bind(this));
        }

        componentDidMount() {
            this.getWalletInfo();
        }

        onHideBalance() {
            let walletInfo = this.state.WalletInfo;
            if(walletInfo!=null) {
                axios.get('/api/Account/SetHideBalance',{ params: {value:!walletInfo.isHideBalance} });
                walletInfo.isHideBalance = !walletInfo.isHideBalance;
                this.setState({ WalletInfo: walletInfo},function (){
                    let balance = i18n.lbl_Currency;
                    if(!walletInfo.isHideBalance){
                        balance = balance + ' ' +number_format(walletInfo.balance.toFixed(2));
                    }
                    $(".WalletInfo").html(balance);
                });
            }
        }
    
        render() {
            let walletInfo = this.state.WalletInfo;
            let tpWallets = null;
            if(this.state.WalletInfo != null && this.state.WalletInfo.tpWallets!=null) {
                tpWallets = this.state.WalletInfo.tpWallets.map(function( item ,index ) { 
                    return <div key={item.tpId} className="wallet-info__item">
                        <span className="text">{item.tpName}</span>
                        <span className="text-amount">{number_format(item.balance.toFixed(2))}</span>
                    </div>
                });
            }

            return (
                <div className="balance-info">
                    <div className="balance-info__option">
                        <label className="checkbox">
                            <input type="checkbox" checked={walletInfo!=null? !walletInfo.isHideBalance: true} onClick={this.onHideBalance.bind(this)} readOnly/>
                            <i className="icon icon-checkbox"></i>
                            <span className="text">{i18n.lbl_ShowBalance}</span>
                        </label>
                    </div>
                    <div className="wallet-info">
                        <div className="wallet-info__item">
                            <span className="text text--lg">{i18n.lbl_Currency}</span>
                            <span className="text-amount text--lg">{walletInfo != null ? number_format(walletInfo.balance.toFixed(2)) : null}</span>
                            <a className="btn btn-icon" data-refresh={this.state.refresh} onClick={this.state.refresh ? null:this.getWalletInfo.bind(this)}>
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
                        {tpWallets}
                    </div>
                        {<div className="btn-group">
                            <a className="btn btn-secondary" href="/Cashier/Deposit">
                                <span className="text">{i18n.lbl_Deposit}</span>
                            </a>
                            <a className="btn btn-secondary" href="/Cashier/Transfer">
                                    <span className="text">{i18n.lbl_TransferFunds}</span>
                                </a>
                        </div>}
                    <div className="login-info">
                        <div className="login-info__item">
                            <span className="text">{i18n.lbl_LastLogin}</span>
                            <span className="text">{walletInfo != null ? moment(walletInfo.lastLoginTime).format('l LTS'):null}</span>
                        </div>
                        <div className="login-info__item">
                            <span className="text">{i18n.lbl_LastTransaction}</span>
                            <span className="text">{walletInfo != null && walletInfo.lastTransaction != null ? moment(walletInfo.lastTransaction).format('l LTS'):null}</span>
                        </div>
                    </div>
                </div>                
            )
        }
    }
    let _walletInfo = <WalletInfo/>;
    ReactDOM.render(_walletInfo, document.getElementById('Top_WalletInfo'));
    
    })(jQuery, React, ReactDOM, i18n, axios, moment);