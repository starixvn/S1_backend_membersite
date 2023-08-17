import {Page} from './dataDefs';
import {Provider} from '../../../../datas/Provider';
import {GameElem} from './components/gameElem';
import {GameList} from './gameList';
import {Nav} from './nav';
import {GamesByProvider, AllGames} from '../../../../datas/AllGameList';
import InfiniteScroll from '../../../../../../../webpack/node_modules/react-infinite-scroller/dist/InfiniteScroll';

(function ($, React, ReactDOM, i18n) {

    const MobileAndTabletCheck = ()=>{
        let check = false;
        (function(a){if(/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(a)||/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0,4))) check = true;})(navigator.userAgent||navigator.vendor||window.opera);
        return check;
    };

    function getUrlParameter(name) {
        name = name.replace(/[\[]/, '\\[').replace(/[\]]/, '\\]');
        var regex = new RegExp('[\\?&]' + name + '=([^&#]*)','i');
        var results = regex.exec(location.search);
        return results === null ? '' : decodeURIComponent(results[1].replace(/\+/g, ' '));
    }

    const IsMobileAndTablet = MobileAndTabletCheck();

    const _AllGames = AllGames().filter(g=>{return TpIds === null ? isLogin === '1' ? false : true : TpIds.includes(g.provider)});
    const _GamesByProvider = {};

    class Panel extends React.Component {
        constructor(props) {
            super(props);

            let defaultTpId = getUrlParameter('tpid');
            let _defaultProvider = null;
            if(defaultTpId && defaultTpId !== '0'){
                let _provider = Provider.find(x=> x.value == defaultTpId);
                _defaultProvider = _provider ? _provider : i18n.lbl_Provider;
            }
            
            const defaultPage = _defaultProvider || defaultTpId==='0' ? Page.PROMO : Page.PROMO;
            const defaultProvider = _defaultProvider ? _defaultProvider : {value: 'Provider', displayText:i18n.lbl_Provider};

            this.state = {
                Filter:{
                    Page:defaultPage,
                    Provider:defaultProvider,
                    Keyword:""
                },
                Games:{
                    init:true,
                    hasMore:true,
                    loadedCount:0,
                    TopSection:[],
                    SecondSection:[]
                },
                Favorite:FavoriteGames
            };

            this.setMouseEvent = this.setMouseEvent.bind(this);
            this.HandleFilter = this.HandleFilter.bind(this);
            this.SearchGame = this.SearchGame.bind(this);
            this.MoreGames = this.MoreGames.bind(this);
            this.GetGameList = this.GetGameList.bind(this);
            this.HandleFavourite = this.HandleFavourite.bind(this);
        }
        get RowsPerPage(){return 12;}

        setMouseEvent = ()=>{
            if(!IsMobileAndTablet){
                $(".BIG,.MI > div,.SM > div").mouseover(function () {
                    $(this).children(".playNow").show();
                });
                $(".BIG,.MI > div,.SM > div").mouseout(function () {
                    $(this).children(".playNow").hide()
                });
            }
        }
        componentDidMount=()=>{
            this.setMouseEvent();
        }
        componentDidUpdate=()=>{
            this.setMouseEvent();
        }
        SetGames = (games, _favorite)=>{
            let first = [];
            let second = [];

            let idx = 0;
            let endPoint = 1+10+this.RowsPerPage;

            for(let i in games){
                let isFavorite = _favorite !== null && _favorite.find(x=> x.ProviderId==games[i].provider && x.GameId==games[i].gameId) !== undefined;
                let {value, name, displayText, ...providerProps} = Provider.find(x=> x.value === games[i].provider);
                let elem = (
                    <GameElem
                        key={`P-${games[i].provider}-Game-${games[i].gameId}`}
                        GameName={games[i].name[langCode] ?? games[i].name['en']}
                        ProviderId={games[i].provider}
                        ProviderName={games[i].providerName}
                        ProviderProps={providerProps}
                        ImgUrl={games[i].imgName[langCode] ?? games[i].imgName['en']}
                        GameId={games[i].gameId}
                        HandleFavourite={this.HandleFavourite.bind(null, games[i].provider, games[i].gameId)}
                        IsFavorite={isFavorite}
                        BigItem={first.length === 0}
                        IsMobileAndTablet={IsMobileAndTablet}
                        Isfun={games[i].Isfun}
                    />
                );

                if(first.length === 0){
                    first.push(elem);
                }else{
                    second.push(elem);
                }

                idx += 1;
                if(idx === endPoint){
                    break;
                }
            }

            let loadedCount = first.length + second.length;
            let matchCount = games.length;
            return {first:first, second:second, loadedCount:loadedCount, hasMore:matchCount > loadedCount}
        }
        HandleFilter = (v, dataColumn)=>{
            if(this.state.Filter[dataColumn] !== v){
                if(dataColumn === 'Page' || dataColumn === "Provider"){
                    let games = dataColumn==='Page' ? this.GetGameList(v, this.state.Filter.Keyword, this.state.Filter.Provider, this.state.Favorite) : this.GetGameList(this.state.Filter.Page, this.state.Filter.Keyword, v, this.state.Favorite);
                    let gameResult = this.SetGames(games, this.state.Favorite);
                    this.setState({
                        Filter:{
                            ...this.state.Filter,
                            [dataColumn]:v,
                        },
                        Games:{
                            ...this.state.Games,
                            hasMore:gameResult.hasMore,
                            loadedCount:gameResult.loadedCount,
                            TopSection:gameResult.first,
                            SecondSection:gameResult.second
                        }
                    });
                }else{
                    this.setState({
                        Filter:{
                            ...this.state.Filter,
                            [dataColumn]:v
                    }});
                }
            }
        }
        GetGameList = (_page, _keyword, _provider, _favorite)=>{
            let allGames = [];
            if(_provider.value === 'All' || _provider.value === 'Provider'){
                allGames = [..._AllGames];
            }else if(_GamesByProvider[_provider.value]){
                allGames = [..._GamesByProvider[_provider.value]];
            }else{
                allGames = GamesByProvider(_provider);
                _GamesByProvider[_provider.value] = [...allGames];
            }

            switch(_page.displayText) {
                case Page.ALLGAMES.displayText:
                    return _keyword === "" ? allGames : allGames.filter(g=>{
                        return this.matchKeyword(g.name, _keyword);
                    });
                
                case Page.FEATURED.displayText:
                    return allGames.filter(g=>{
                        return g.featured===true && (_keyword === "" || this.matchKeyword(g.name, _keyword));
                    });

                case Page.FAVOURITE.displayText:
                    return allGames.filter(g=>{
                        return _favorite !== null && _favorite.find(x=> x.ProviderId==g.provider && x.GameId==g.gameId) !== undefined;
                    });
                
                case Page.PROMO.displayText:
                    return allGames.filter(g=>{
                        return g.promotion==1 && (_keyword === "" || this.matchKeyword(g.name, _keyword));
                    });

                default:
                    return allGames.filter(g=>{
                        return g.category1===_page.category1 && (_keyword === "" || this.matchKeyword(g.name, _keyword));
                    });;
            }
        }
        matchKeyword = (name, _keyword)=>{
            let keyword = _keyword.toLowerCase();
            return Object.keys(name).filter(key=>{return name[key].toLowerCase().indexOf(keyword) !== -1}).length > 0;
        }
        SearchGame = ()=>{
            let keyword = this.state.Filter.Keyword;
            let game = this.GetGameList(this.state.Filter.Page, keyword, this.state.Filter.Provider, this.state.Favorite);
            
            let gameResult = this.SetGames(game, this.state.Favorite);
            this.setState({
                Games:{
                    ...this.state.Games,
                    hasMore:gameResult.hasMore,
                    loadedCount:gameResult.loadedCount,
                    TopSection:gameResult.first,
                    SecondSection:gameResult.second
                }
            });
        }
        MoreGames=()=>{
            if(this.state.Games.hasMore){
                let games = this.GetGameList(this.state.Filter.Page, this.state.Filter.Keyword, this.state.Filter.Provider, this.state.Favorite);
                let gameLength = games.length;
                let restItemCount = gameLength - this.state.Games.loadedCount;
                let getItem = this.RowsPerPage >= restItemCount ? restItemCount : this.RowsPerPage;
                let SecondSection = [];
                let TopSection = [...this.state.Games.TopSection];

                let startIdx = this.state.Games.loadedCount;
                let endIdx = startIdx + getItem;
                
                for(let i=startIdx; i<endIdx; i++){
                    let key=`P-${games[i].provider}-Game-${games[i].gameId}`;
                    let isFavorite = this.state.Favorite !== null && this.state.Favorite.find(x=> x.ProviderId==games[i].provider && x.GameId==games[i].gameId) !== undefined;
                    let {value, name, displayText, ...providerProps} = Provider.find(x=> x.value === games[i].provider);
                    if(TopSection.length === 0){
                        TopSection.push(
                            <GameElem
                                key={key}
                                GameName={games[i].name[langCode] ?? games[i].name['en']}
                                ProviderId={games[i].provider}
                                ProviderName={games[i].providerName}
                                ProviderProps={providerProps}
                                ImgUrl={games[i].imgName[langCode] ?? games[i].imgName['en']}
                                GameId={games[i].gameId}
                                HandleFavourite={this.HandleFavourite.bind(null, games[i].provider, games[i].gameId)}
                                IsFavorite={isFavorite}
                                BigItem={true}
                                IsMobileAndTablet={IsMobileAndTablet}
                                Isfun={games[i].Isfun}
                            />
                        );
                    }else{
                        SecondSection.push(
                            <GameElem
                                key={key}
                                GameName={games[i].name[langCode] ?? games[i].name['en']}
                                ProviderId={games[i].provider}
                                ProviderName={games[i].providerName}
                                ProviderProps={providerProps}
                                ImgUrl={games[i].imgName[langCode] ?? games[i].imgName['en']}
                                GameId={games[i].gameId}
                                HandleFavourite={this.HandleFavourite.bind(null, games[i].provider, games[i].gameId)}
                                IsFavorite={isFavorite}
                                BigItem={false}
                                IsMobileAndTablet={IsMobileAndTablet}
                                Isfun={games[i].Isfun}
                            />
                        );
                    }
                }
                
                let loadedCount = this.state.Games.loadedCount + getItem;
                this.setState({
                    Games:{
                        ...this.state.Games,
                        init:false,
                        TopSection:[...TopSection],
                        SecondSection:[...this.state.Games.SecondSection, ...SecondSection],
                        loadedCount:loadedCount,
                        hasMore:gameLength !== loadedCount
                    }
                })
            }
        }
        HandleFavourite = (providerId, gameId)=>{
            let _fav = this.state.Favorite ?? [];
            let idx = _fav.findIndex(x=> x.ProviderId==providerId && x.GameId==gameId);

            let isRemove = idx !== -1 ;
            if(isLogin === '1'){
                axios.post('/api/Account/SetFavoriteGame', {ProviderId: Number(providerId), GameId:gameId, IsRemove:isRemove});
            }

            if(isRemove){
                if(this.state.Filter.Page.displayText === Page.FAVOURITE.displayText){
                    let datas = [...this.state.Games.TopSection, ...this.state.Games.SecondSection].filter(item=>{
                        return item.key !== `P-${providerId}-Game-${gameId}`;
                    });
                    let TopSection = [];
                    let SecondSection = [];

                    datas.forEach(g => {
                        if(TopSection.length===0){
                            TopSection.push(g);
                        }else{
                            SecondSection.push(g);
                        }
                    });

                    _fav.splice(idx, 1);
                    this.setState({
                        Games:{
                            ...this.state.Games,
                            loadedCount:TopSection.length+SecondSection.length,
                            TopSection:TopSection,
                            SecondSection:SecondSection
                        },
                        Favorite:_fav
                    });
                }else{
                    _fav.splice(idx, 1);
                    this.setState({
                        Favorite:_fav
                    });
                }
            }else{
                _fav.push({ProviderId: providerId, GameId:gameId});
                this.setState({
                    Favorite:_fav
                });
            }
        }
        render(){
            const loader = <div className="loader" key={`load-${this.state.Games.loadedCount}`}>Loading ...</div>;
            return(
                <div className="page CasinoSelect">
                    <Nav
                        HandleFilter={this.HandleFilter}
                        SelectedProvider={this.state.Filter.Provider}
                        SelectedPage={this.state.Filter.Page}
                        SearchGame={this.SearchGame}
                    />
                    {
                        !this.state.Games.init && this.state.Games.loadedCount === 0 ? <NoData /> : (
                            <InfiniteScroll
                                pageStart={0}
                                loadMore={this.MoreGames}
                                hasMore={this.state.Games.hasMore}
                                loader={loader}>
                                <GameList TopSection={this.state.Games.TopSection} SecondSection={this.state.Games.SecondSection}/>
                            </InfiniteScroll>
                        )
                    }
                </div>
            );
        }
    }

    const NoData = ()=>{
        return(
            <div className="CasModule">
                <div className="not-found" style={{display:'block'}}>{i18n.lbl_NoMatching}</div>
            </div>
        );
    }

    ReactDOM.render(<Panel />, document.getElementById('casino_content'));
})(jQuery, React, ReactDOM, i18n, axios, moment);