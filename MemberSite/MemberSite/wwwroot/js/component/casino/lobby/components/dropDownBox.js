export class DropdownBox extends React.Component{
    constructor(props){
        super(props)
        this.state = {isOpen:false}
        this.ClickDropdown = this.ClickDropdown.bind(this);
        this.handleClickOutside = this.handleClickOutside.bind(this);
        this.SelectItem = this.SelectItem.bind(this);
        document.addEventListener('mousedown', this.handleClickOutside);
    }
    handleClickOutside(e) {
        if (this.wrapper && !this.wrapper.contains(e.target)) {
            this.setState({isOpen:false});
        }
    }
    SetOuterDivRef=(node)=>{
        this.wrapper = node;
    }
    ClickDropdown=()=>{
        this.setState({isOpen: !this.state.isOpen})
    }
    SelectItem=(v)=>{
        this.setState({isOpen:false})
        if(this.props.OnSelectItem !== undefined){
            this.props.OnSelectItem(v, this.props.DataColumn, this.props.BindingValue);
        }
    }
    render(){
        let selectedValue = this.props.SelectedItem !== undefined && this.props.SelectedItem !== null ? typeof(this.props.SelectedItem) === 'object' ? this.props.SelectedItem.value : this.props.SelectedItem : '';
        let rows = this.props.Options && this.props.Options.map((item, idx)=>{            
            let value = item && typeof(item) === 'object' ? item.value : item;
            let displayText = item && typeof(item) === 'object' ? item.displayText : item;
            return(
                <div key={`dp-${value}-${idx}`} className="dropdown__item" onClick={this.SelectItem.bind(null, item)} data-selected={selectedValue === value}>
                    <span className="text">{displayText}</span>
                </div>
            );
        });

        let elem = (
            <div className={`dropdown dropdown-default${this.props.SubClass ? ' '+this.props.SubClass : ''}`} ref={this.SetOuterDivRef} data-open={this.state.isOpen}>
                <a className="btn" onClick={this.ClickDropdown}>
                    <span className="text">{selectedValue}</span>
                    <i className="icon icon-caret"></i>
                </a>
                <div className="dropdown__container">
                    <div className="dropdown__list">
                        {rows}
                    </div>
                </div>
            </div>
        );

        return elem
    }
}