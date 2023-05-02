import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios) {

    class BalancePanel extends React.Component {
        //data-status="error" 1.info, 2.error,3.valid
        constructor(props) {
            super(props);
            this.state = { refresh: false, amount: null};
        }
    
        getUserBalance() {
            this.setState({ refresh: true});
            axios.post('/api/Account/GetBalance')
            .then(
                function(response) {
                    var result = response.data;
                    if (result.errorCode != null && result.errorCode == 0) {
                        this.setState({ refresh: false, amount: number_format(result.data.toFixed(2))},function(){
                            $(".Js-Balance").html(number_format(result.data.toFixed(2)));
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
            this.getUserBalance();
        }
    
        render() {
            let amount = this.state.amount;

            return (
                <div className="account-info__row">
                    <span className="account-info__currency">{i18n.lbl_Currency}</span>
                    <span className="account-info__amount Js-Balance">{amount}</span>
                    <button className="btn btn-icon" data-refresh={this.state.refresh} onClick={this.state.refresh?null:this.getUserBalance.bind(this)}>
                        <i className="icon icon-refresh"></i>
                        <div className="preloader">
                            <div className="preloader__spiner">
                                <div className="preloader__circles">
                                    <div className="preloader__circle-1"></div>
                                    <div className="preloader__circle-2"></div>
                                </div>
                            </div>
                        </div>
                    </button>
                </div>
            )
        }
    }
    let _balancePanel = <BalancePanel/>;
    ReactDOM.render(_balancePanel, document.getElementById('BalancePanel'));
    ReactDOM.render(_balancePanel, document.getElementById('BalancePanel1'));
    
    })(jQuery, React, ReactDOM, i18n, axios);