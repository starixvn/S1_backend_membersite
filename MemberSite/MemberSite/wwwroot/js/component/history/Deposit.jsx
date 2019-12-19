(function ($, React, ReactDOM, i18n, axios, moment) {
    class DepositHistory extends React.Component {
        constructor(props) {
            super(props);
            this.state={"DipositType": 0, showModal: false };
        }

        onWireTransfers() {
            this.setState({"DipositType": 1});
        }

        onDepositOptions() {
            this.setState({"DipositType": 0});
        }

        onCloseModal() {
            this.setState({showModal:false});
        }
        
        onShowBankModal(bankInfo) {
            this.setState({showModal:true, bankInfo: bankInfo,DipositType: 0});
        }
   
        render() {
            let panelHolder = <DepositOptions onWireTransfers={this.onWireTransfers.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)}/>;
            if(this.state.DipositType == 1) {
                panelHolder = <WireTransfers onDepositOptions={this.onDepositOptions.bind(this)} onShowBankModal={this.onShowBankModal.bind(this)}/>;
            } 

            let modalContaner=null;

            modalContaner = this.state.showModal ? <BankModal bankInfo={this.state.bankInfo} onCloseModal={this.onCloseModal.bind(this)}/> : null;

            return (
                <div>{panelHolder}{modalContaner}</div>
            )
        }
    }
    
    ReactDOM.render(<DepositHistory/>,document.getElementById('History'));
    })(jQuery, React, ReactDOM, i18n, axios, moment);