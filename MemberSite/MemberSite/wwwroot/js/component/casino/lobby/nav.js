import {Page} from './dataDefs';
import {DropdownBox} from './components/dropDownBox';
import {Provider} from '../../../../datas/Provider';

export const Nav = (props)=>{
    let desktopPages = [];
    let mobilePages = [];
    Object.keys(Page).forEach((key, idx) => {
        let pageInfo = Page[key];
        let active = props.SelectedPage.displayText === pageInfo.displayText;
        desktopPages.push(
            <a key={`d-p-${idx}`} onClick={props.HandleFilter.bind(null, pageInfo, 'Page')} className={active ? 'active' : ''}>
                <span>{pageInfo.displayText}</span>
            </a>
        );

        mobilePages.push(pageInfo.displayText);
    });

    const InputKeyword = (e)=>{
        props.HandleFilter(e.target.value, "Keyword");
    }

    const OnKeyPress = (e)=>{
        if(e.key==='Enter'){
            props.SearchGame();
        }
    }

    return(
        <div className="CasinoSelect_nav" role="desktop-only">
            {desktopPages}
            <DropdownBox 
                OnSelectItem={props.HandleFilter}
                DataColumn="Page"
                Options={mobilePages}
                SelectedItem={props.SelectedPage}
                SubClass="mobile-sort-select"
            />
            <div className="textfield">
                <input type="text" className="textfield__input" defaultValue="" onChange={InputKeyword} onKeyPress={OnKeyPress}/>
                <span className="textfield__bg" style={{background: "#fff"}}></span>
                <a className="btn btn-icon" data-visibility="true" onClick={props.SearchGame}>
                    <i className="icon icon-search"></i>
                </a>
            </div>
            <DropdownBox 
                OnSelectItem={props.HandleFilter}
                DataColumn="Provider"
                Options={[{value:'All', name:'All', displayText:i18n.lbl_All}, ...Provider.filter(p=>{return TpIds === null ? isLogin === '1' ? false : true : TpIds.includes(p.value)})]}
                SelectedItem={props.SelectedProvider.displayText}
                HasAll={true}
            />
        </div>
    )
}