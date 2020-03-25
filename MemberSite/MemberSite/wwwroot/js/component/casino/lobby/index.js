import {Page} from './dataDefs';
import {Provider} from '../../../../datas/Provider';
import {GameElem} from './components/gameElem';
import {GameList} from './gameList';
import {Nav} from './nav';
import {GamesByProvider, AllGames} from '../../../../datas/AllGameList'

(function ($, React, ReactDOM, i18n) {
    class Panel extends React.Component {
        constructor(props) {
            super(props);

            const defaultPage = Page.ALLGAMES.displayText;
            const defaultProvider = {value:'Provider', displayText:'Provider'};

            let games = this.GetGameList(defaultPage, "", defaultProvider, FavoriteGames);
            let gameResult = this.SetGames(games, FavoriteGames===null? [] : FavoriteGames);
            
            this.state = {
                Filter:{
                    Page:defaultPage,
                    Provider:defaultProvider,
                    Keyword:""
                },
                Games:{
                    hasMore:gameResult.hasMore,
                    loadedCount:gameResult.loadedCount,
                    TopSection:gameResult.first,
                    SecondSection:gameResult.second
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
        get RowsPerPage(){return 24;}

        setMouseEvent = ()=>{
            $(".BIG,.MI > div,.SM > div").mouseover(function () {
                $(this).children(".playNow").show()
            });
            $(".BIG,.MI > div,.SM > div").mouseout(function () {
                $(this).children(".playNow").hide()
            });
        }
        componentDidMount=()=>{
            this.setMouseEvent();
        }
        componentDidUpdate=()=>{
            this.setMouseEvent();
        }
        componentWillMount=()=>{
            window.addEventListener('scroll', this.MoreGames);
        }
        componentWillUnmount=()=>{
            window.removeEventListener('scroll', this.MoreGames);
        }
        SetGames = (providerGames, _favorite)=>{
            let first = [];
            let second = [];

            let idx = 0;
            let endPoint = 1+10+this.RowsPerPage;
            let _break = false;
            for(let p in providerGames){
                let games = providerGames[p].data;
                for(let i in games){
                    let isFavorite = _favorite !== null && _favorite.find(x=> x.ProviderId==providerGames[p].provider && x.GameId==games[i].gameId) !== undefined;
                    let elem = (
                        <GameElem
                            key={`P-${providerGames[p].provider}-Game-${games[i].gameId}`}
                            GameName={games[i].name[langCode] ?? games[i].name['en']}
                            ProviderId={providerGames[p].provider}
                            ProviderName={providerGames[p].providerName}
                            ImgUrl={games[i].imgName[langCode] ?? games[i].imgName['en']}
                            GameId={games[i].gameId}
                            HandleFavourite={this.HandleFavourite.bind(null, providerGames[p].provider, games[i].gameId)}
                            IsFavorite={isFavorite}
                            BigItem={first.length === 0}
                        />
                    );

                    if(first.length === 0){
                        first.push(elem);
                    }else{
                        second.push(elem);
                    }

                    idx += 1;
                    if(idx === endPoint){
                        _break = true;
                        break;
                    }
                }

                if(_break) break;
            }

            let loadedCount = first.length + second.length;
            let matchCount = this.GameLength(providerGames);
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
            let allGames = _provider.value === 'All' || _provider.value === 'Provider' ? AllGames() : GamesByProvider(_provider);
            switch(_page) {
                case Page.ALLGAMES.displayText:
                    return _keyword === "" ? allGames : allGames.map(p=> {
                        return {
                            providerName:p.providerName,
                            provider:p.provider,
                            data: p.data.filter(g=>{
                                return this.matchKeyword(g.name, _keyword);
                            })
                        }
                    });

                case Page.SLOTS.displayText:
                    return allGames.map(p=> {
                        return {
                            providerName:p.providerName,
                            provider:p.provider,
                            data: p.data.filter(g=>{
                                return g.category1==="Slots" && (_keyword === "" || this.matchKeyword(g.name, _keyword));
                            })
                        }
                    });

                case Page.FEATURED.displayText:
                    return allGames.map(p=> {
                        return {
                            providerName:p.providerName,
                            provider:p.provider,
                            data: p.data.filter(g=>{
                                return g.featured===true && (_keyword === "" || this.matchKeyword(g.name, _keyword));
                            })
                        }
                    });

                case Page.FAVOURITE.displayText:
                    return allGames.map(p=> {
                        return {
                            providerName:p.providerName,
                            provider:p.provider,
                            data: p.data.filter(g=>{
                                return _favorite !== null && _favorite.find(x=> x.ProviderId==p.provider && x.GameId==g.gameId) !== undefined;
                            }).map(item=>{
                                return {
                                    ...item,
                                    ProviderId: p.providerId
                                }
                            })
                        }
                    });

                default:
                    return allGames;
            }
        }
        matchKeyword = (name, _keyword)=>{
            let keyword = _keyword.toLowerCase();
            return Object.keys(name).filter(key=>{return name[key].indexOf(keyword) !== -1}).length > 0;
        }
        SearchGame = ()=>{
            let keyword = this.state.Filter.Keyword;
            let game = this.GetGameList(this.state.Filter.Page, keyword, this.state.Filter.Provider, this.state.Favorite);
            
            let gameResult = this.SetGames(game, this.state.Favorite);
            this.setState({
                Games:{
                    hasMore:gameResult.hasMore,
                    loadedCount:gameResult.loadedCount,
                    TopSection:gameResult.first,
                    SecondSection:gameResult.second
                }
            });
        }
        GameLength=(providerGames)=>{
            let result = 0;
            for(let p in providerGames){
                result += providerGames[p].data.length
            }

            return result;
        }
        MoreGames=()=>{
            if ((window.innerHeight + document.documentElement.scrollTop + $($(".footer").get(0)).height()) >= document.scrollingElement.scrollHeight && this.state.Games.hasMore) {
                let providerGames = this.GetGameList(this.state.Filter.Page, this.state.Filter.Keyword, this.state.Filter.Provider, this.state.Favorite);
                let currIdx = this.state.Games.loadedCount-1;
                let gameLength = this.GameLength(providerGames);
                let restItemCount = gameLength - this.state.Games.loadedCount;
                let getItem = this.RowsPerPage >= restItemCount ? restItemCount : this.RowsPerPage;
                let SecondSection = [];

                let pivot = 0;
                let startIdx = currIdx;
                let endPoint = currIdx+getItem;
                let _break = false;
                for(let p in providerGames){
                    let games = providerGames[p].data;
                    let idx = pivot + games.length - 1;
                    let start = 0;
                    let end = 0;

                    if(idx >= startIdx){
                        start = Math.abs(idx-games.length-startIdx);
                        end = games.length > endPoint ? endPoint+1 : games.length;
                    }

                    if(start !== end){
                        for(let i=start; i<end; i++){
                            let key=`P-${providerGames[p].provider}-Game-${games[i].gameId}`;
                            let isFavorite = this.state.Favorite !== null && this.state.Favorite.find(x=> x.ProviderId==providerGames[p].provider && x.GameId==games[i].gameId) !== undefined;;
                            SecondSection.push(
                                <GameElem
                                    key={key}
                                    GameName={games[i].name[langCode] ?? games[i].name['en']}
                                    ProviderId={providerGames[p].provider}
                                    ProviderName={providerGames[p].providerName}
                                    ImgUrl={games[i].imgName[langCode] ?? games[i].imgName['en']}
                                    GameId={games[i].gameId}
                                    HandleFavourite={this.HandleFavourite.bind(null, providerGames[p].provider, games[i].gameId)}
                                    IsFavorite={isFavorite}
                                    BigItem={false}
                                />
                            );

                            if(i===endPoint){
                                _break = true;
                                break;
                            }
                        }

                        if(_break){ 
                            break; 
                        }else{
                            pivot += games.length - SecondSection.length;
                        }
                    }else{
                        pivot += games.length;
                    }
                }

                let loadedCount = this.state.Games.loadedCount + getItem;
                this.setState({
                    Games:{
                        ...this.state.Games,
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
                if(this.state.Filter.Page === Page.FAVOURITE.displayText){
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
                            loadedCount:TopSection.length+SecondSection.length,
                            TopSection:TopSection,
                            SecondSection:SecondSection
                        },
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
            return(
                <div className="page CasinoSelect">
                    <Nav
                        HandleFilter={this.HandleFilter}
                        SelectedProvider={this.state.Filter.Provider}
                        SelectedPage={this.state.Filter.Page}
                        SearchGame={this.SearchGame}
                    />
                    {
                        this.state.Games.loadedCount === 0 ? <NoData /> : (
                            <GameList TopSection={this.state.Games.TopSection} SecondSection={this.state.Games.SecondSection}/>
                        )
                    }
                </div>
            );
        }
    }

    const NoData = ()=>{
        return(
            <div className="CasModule">
                <div className="not-found" style={{display:'block'}}>No matching products found.</div>
            </div>
        );
    }

    ReactDOM.render(<Panel />, document.getElementById('casino_content'));
})(jQuery, React, ReactDOM, i18n, axios, moment);