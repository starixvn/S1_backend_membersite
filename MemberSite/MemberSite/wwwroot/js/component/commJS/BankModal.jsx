import {number_format} from '../commJS/common';
export class BankModal extends React.Component {
        _clipboard=null;
        constructor(props) {
            super(props);
            this.state={Show: false};
        }

        componentDidMount() {
            this._clipboard = new ClipboardJS(".copyBtn");
            /*clipboard = new ClipboardJS(this.btn2);
            clipboard = new ClipboardJS(this.btn3);
            clipboard = new ClipboardJS(this.btn4);*/
            this._clipboard.on('success', function(e) {
                //console.log(e);
            });
            this._clipboard.on('error', function(e) {
                //console.log(e);
            });
            this.setState({Show: true});
        }

        componentWillUnmount() {
            this._clipboard.destroy();
        }

        onClose(){
            this.props.onCloseModal();
        }

        render() {
            let bankInfo = this.props.bankInfo;

            return (
            <div className="modal" data-open={this.state.Show}>
                <div className="overlay"></div>
                <div className="modal__dialog">
                    <div className="modal__header">
                        <span className="text">{i18n.lbl_Transaction}</span>
                    </div>
                    <div className="modal__body">
                        <p>{i18n.lbl_TransactionHint}</p>
                        <div className="table table-modal">
                            <div className="tbody">
                                <div className="tr">
                                    <div className="td-left">
                                        <div className="payment">
                                            <div className="payment__img">
                                                <img src={"/common/images/BankLogo/"+bankInfo.bankLogo}/>
                                            </div>
                                            <div className="payment__name">
                                                <span className="text">{bankInfo.bankName}</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="td-right">
                                        <a className="btn btn-icon text-primary" target="_blank" href={bankInfo.bankSite}>
                                            <i className="icon icon-new-window"></i>
                                        </a>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankAccountName}</span>
                                        <span className="text">{bankInfo.accountName}</span>
                                    </div>
                                    <div className="td-right">
                                        <a className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.accountName}>
                                            <i className="icon icon-copy"></i>
                                            {/*<div className="tooltip tooltip--top-right" data-open="true">
                                                <span className="text">copy</span>
                                            </div>*/}
                                        </a>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_BankNumber}</span>
                                        <span className="text">{bankInfo.accountNo}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.accountNo}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_DepositAmount}</span>
                                        <span className="text">{number_format(parseFloat(bankInfo.amount).toFixed(2))}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={parseFloat(bankInfo.amount)}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>                                
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text-lowlight">{i18n.lbl_Postscript}</span>
                                        <span className="text-accent">{bankInfo.postScript}</span>
                                    </div>
                                    <div className="td-right">
                                        <button className="btn btn-icon text-primary copyBtn" data-clipboard-text={bankInfo.postScript}><i className="icon icon-copy"></i></button>
                                    </div>
                                </div>
                            </div>
                            <div className="tfoot">
                                <div className="tr">
                                    <div className="td-left">
                                        <span className="text">{i18n.lbl_PostscriptHint}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div className="btn-group">
                            <button className="btn btn-primary" onClick={this.onClose.bind(this)} type="button">
                                <div className="btn__inner">
                                    <span className="text">{i18n.lbl_Close}</span>
                                </div>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            )
        }
    }