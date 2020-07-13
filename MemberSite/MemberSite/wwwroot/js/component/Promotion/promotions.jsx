
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment) {
    const Status = {1:<div className="status-icon ace">Running</div>,
                    2:<div className="status-icon cal">Canceled</div>,
                    3:<div className="status-icon cal">Expiry</div>,
                    4:<div className="status-icon end">Fulfill</div>};   

    class PromotionItem extends React.Component {
        constructor(props) {
            super(props);
        }

        render() {
            let Data=this.props.Data;
            const diff = moment.duration(moment.unix(Data.periodTo).diff(new Date()));
            return (
                <a href={"/"+SiteLanguage+"/Promotion/Bonus?BonusCode="+Data.bonusCode}>
                    <img src={CMSSkinRootPath+"Promotion/"+SiteLanguage+"/banner-"+Data.bonusCode+".jpg"}/>
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
                        //PromotionList.push(<span key={iKey} className="promotions-title">{iData.categoryName}</span>);
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