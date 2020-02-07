
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment) {
    class PromotionItem extends React.Component {
        constructor(props) {
            super(props);
        }

        render() {
            let Data=this.props.Data;

            return (
                <a href={"/"+SiteLanguage+"/Promotion/Bonus?BonusCode="+Data.bonusCode} className="promotions-set">
                    <article>
                        <div className="promotions-banner"></div>
                        <div className="promotions-txt">
                            <ul>
                                <li className="txt-blue">{Data.title}</li>
                                <li className="txt-gray">
                                    {Data.description}
                                </li>
                                <li>
                                    <div className="promotions-type">
                                        <ul className="txt-gray">
                                            <li>
                                                <i className="icon icon-transaction"></i>BONUS
                                                <span className="txt-org">{i18n["Currency_"+Data.currencyId]} {Data.bonus}%</span>
                                            </li>
                                            <li>
                                                <i className="icon icon-gototop"></i>MAX
                                                <span className="txt-org">{i18n["Currency_"+Data.currencyId]} {number_format(Data.maxBonus)}</span>
                                            </li>
                                            <li>
                                                <i className="icon icon-sport252"></i>ROLL OVER
                                                <span className="txt-org">{Data.rollOver} X</span>
                                            </li>
                                            {Data.bonusInfoStatus!=1 ?
                                            <li>
                                                <i className="icon icon-race"></i>REMAINING TIME
                                                <span className="txt-org">{moment.unix(Data.periodTo).format('lll')}</span>
                                            </li>:<li>
                                                <i className="icon icon-race"></i>EXPRIY TIME
                                                <span className="txt-org">{moment(Data.expiryTime).format('lll')}</span>
                                            </li>}
                                            {Data.bonusInfoStatus==1 ?
                                            <li>
                                                <i className="icon icon-transaction"></i>bonus Amount
                                                <span className="txt-org">{number_format(Data.bonusAmount)}</span>
                                            </li>:null}                                            
                                        </ul>
                                    </div>                                
                                </li>
                                <li className="txt-org">{i18n.lbl_TC}</li>                                
                            </ul>
                        </div>
                    </article>
                    {Data.bonusInfoStatus==1 && Data.custRollOver!=null  ? <article>
                        <div className="schedule-module">
                            <div className="promotions-txt">
                                <ul>
                                    <li className="txt-org">BONUS ROLL OVER</li>
                                    <li className="txt-gray">
                                        <span className="txt-org">{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.bonusTurnOver)}</span> (Req.{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.bonusRollOver)})
                                        <div className="percentage"></div>
                                    </li>
                                    <li>
                                        <div className="schedule">
                                            <div className="schedule-bg" style={{width:  (Data.custRollOver.bonusTurnOver / Data.custRollOver.bonusRollOver) * 100 + "%"}}></div>
                                        </div>
                                    </li>
                                </ul>
                            </div>
                            <div className="promotions-txt">
                                <ul>
                                    <li className="txt-org">DEPOSIT ROLL OVER</li>
                                    <li className="txt-gray">
                                        <span className="txt-org">{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.depositTurnOver)}</span> (Req.{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.depositRollOver)})
                                        <div className="percentage"></div>
                                    </li>
                                    <li>
                                        <div className="schedule">
                                            <div className="schedule-bg" style={{width:  (Data.custRollOver.depositTurnOver / Data.custRollOver.depositRollOver) * 100 + "%"}}></div>
                                        </div>
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </article>:null}    
                </a>
            )
        }
    }

    class Promotions extends React.Component {
        constructor(props) {
            super(props);
            this.state={Data: null};
        }

        componentDidMount() {
            this.getPromotionList();
        }        

        getPromotionList() {
            Loading.OnLoading();
            axios.post('/api/Promotion/GetPromotionList/'+ SiteLanguage)
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        this.setState({ Data: result.data });
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
        }        

        render() {
            let PromotionList = [];
            let categoryId = 0;
            let iKey = 0;
            let data = this.state.Data;
            if(data != null) {
                for(let i=0; i < data.length; i++) {
                    let iData = data[i];
                    if(iData.categoryId != categoryId){
                        PromotionList.push(<span key={iKey} className="promotions-title">{iData.categoryName}</span>);
                        iKey++;
                    }

                    PromotionList.push(<PromotionItem key={iKey} Data={iData}/>);
                }
            }
            //
            return (
                <div className="promotions-module">
                    {PromotionList}
                </div>
            )
        }
    }

    ReactDOM.render(<Promotions />, document.getElementById('promotions'));
})(jQuery, React, ReactDOM, i18n, axios, moment);