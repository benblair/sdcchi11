var peepsByKey = { };
var groups = { };
var excludedGroups = { };
var peepsLoaded = false;
var showUserTweets;

var height = window.innerHeight - 20;
var width = window.innerWidth - 20;
var getX = d3.scale.linear().domain([0,1]).range([20, width - 320]);
var getY = d3.scale.linear().domain([0,1]).range([120,height - 50]);
var getRadius = function(d) { return 15; };
var colorize = d3.scale.linear().domain([0,1]).range(["hsl(250, 50%, 50%)", "hsl(350, 100%, 50%)"]).interpolate(d3.interpolateHsl);
var del = d3.scale.linear().domain([0,1]).range([0,1]);

var canvas = $("#peeps");
var socket;

var makeSafeId = function(s){
    if(s===null){
        return null;
    }
    
    return s.replace(new RegExp("[\\W]","g"),"");
}
    
var drawPeep = function(data) {
    var newPeep = $('<img />');
    newPeep.attr("src", data.pic);
    newPeep.attr("title", data.handle);
    newPeep.attr("alt", data.handle);
    newPeep.attr("id", "img-"+data.handle);
    newPeep.attr("width", "30px");
    newPeep.attr("height", "30px");
    var peepDiv = $('<div class="peep"></div>');
    peepDiv.css("left", (width / 2) + "px");
    peepDiv.css("top", (height / 2) + "px");
    peepDiv.attr("id", data.handle);
    peepDiv.append(newPeep);
    peepDiv.click(function(e) {
        
        showUserTweets(e.currentTarget.id);
    });
    canvas.append(peepDiv);
};

var normalizePeep = function(peep) {
    var origPeep = peepsByKey[peep.Key];

    var newPeep = {
        key: peep.Key,
        x: (peep.X||null==origPeep) ? peep.X : origPeep.X,
        y: (peep.Y||null==origPeep) ? peep.Y : origPeep.Y,
        pic: (peep.ProfilePic ||null==origPeep)? peep.ProfilePic : origPeep.pic,
        handle: (peep.TwitterHandle||null==origPeep) ? peep.TwitterHandle : origPeep.handle,
        name: (peep.RealName||null==origPeep) ? peep.RealName : origPeep.name,
        group: (peep.GroupName||null==origPeep) ? peep.GroupName : origPeep.group,
        groupOld: origPeep?origPeep.group:undefined,
        groupCenterX :(peep.GroupCenterX||null==origPeep) ? peep.GroupCenterX : origPeep.groupCenterX, 
        groupCenterY :(peep.GroupCenterY||null==origPeep) ? peep.GroupCenterY : origPeep.groupCenterY, 
    };
    
    peepsByKey[newPeep.key]=newPeep;
    
    return newPeep;
};

var addGroup = function(groupName,x,y){
    var group = groups[groupName];
    if (!group) {
        group = {
            x: getX(x),
            y: getY(y),
            name: groupName,
            count: 1
        };
        groups[groupName] = group;

        var canvas = $("#groups");
        var groupDiv = $('<div class="group">' + groupName + '</div>');
        groupDiv.click(excludeTerm);
        groupDiv.css("left", (group.x) + "px");
        groupDiv.css("top", (group.y) + "px");
        groupDiv.attr("id", "group-" + makeSafeId(groupName));
        canvas.append(groupDiv);
    }
    else {
        group.count++;
    }
};

var removeUserFromGroup = function(groupName){
    var group = groups[groupName];
    if (null != group) {
        group.count--;
        if (0 == group.count) {
            $('#group-' + makeSafeId(groupName)).remove();
            groups[groupName]=undefined;
        }
    }
};


var addPeep = function(data) {
    var peep = normalizePeep(data);

    addGroup(peep.group,peep.groupCenterX,peep.groupCenterY);

    if (peepsLoaded) {
        drawPeep(peep);
    }
};

var modifyPeep = function(data) {
    var peep = normalizePeep(data);
    var peepDiv = $('#' + peep.handle);
    var changes = { };
    if(data.X) {
        changes.left = getX(peep.x);
    }
    if(data.Y) {
        changes.top = getY(peep.y);
    }
    peepDiv.animate(
        changes,
        {
            duration: 500
        });
    if(data.ProfilePic){
        $('#img-' + peep.handle).attr("src",peep.pic);
    }
        
    var groupChanges = {};
    if(data.GroupCenterX) {
        changes.left = getX(peep.groupCenterX);
    }
    if(data.GroupCenterY) {
        changes.top = getY(peep.groupCenterY);
    }
    
    if(data.GroupCenterX||data.GroupCenterY){
        $('#group-' + makeSafeId(peep.group)).animate(
        changes,
        {
            duration: 1
        });
    }
    
    if(data.GroupName && (peep.group!=peep.groupOld)){
        removeUserFromGroup(peep.groupOld);
        
        addGroup(peep.group,peep.groupCenterX,peep.groupCenterY);
    }
};

var deletePeep = function(data) {
    var peep = normalizePeep(data);
    $('#' + peep.handle).remove();
    
    delete peepsByKey[peep.key];
    
    removeUserFromGroup(peep.group);
};

var updatePeep = function(update) {
    var action = update.action;
    var item = update.item;
    switch (action) {
        case 'add':
            addPeep(item);
            break;
        case 'modify':
            modifyPeep(item);
            break;
        case 'delete':
            deletePeep(item);
            break;
        default:
            return;
    }
};

var connectToApi = function(callback) {
    
    socket = io.connect("https://api.cerrio.com:443");
    
    socket.on("peeps", function(update){
       updatePeep(update);
    });
    
    // First, clear out any existing subscription for this twitter usre
    /*
    socket.emit("update", {
        id: 'login',
        uri: 'SDC/Input/Users',
        action: 'delete',
        itemKey: username
    });
    */
    socket.emit("update", {
        id: 'login',
        uri: 'SDC/Input/Users',
        action: 'add',
        itemKey: username,
        item: {
            Handle: username,
            HashTag: hashtag,
            DeletedTerms: ''
        }
    });
    
    socket.emit("stream", {
        id: "peeps",
        uri: "SDC/Output/Data",
        subscription: "@.OriginatingUser = '" +  username + "'"
    },
    callback);
    
};


showUserTweets = function(handle) {
    var twitterbox = $("#twitter-box");
    twitterbox.css('visibility', 'visible');
    if(twtr) {
        twtr
            .destroy()
            .setFeatures({
                scrollbar: true,
                loop: false,
                live: true,
                hashtags: true,
                timestamp: true,
                avatars: false,
                behavior: 'all'
            })
            .setDimensions(250, 600)
            .setRpp(10)
            .setTweetInterval(30000)
            .setUser(handle)
            .render()
            .start();
    }
};

var excludedWords = [];

var loadPeepsDom = function() {
    peepsLoaded = true;
    canvas = $("#peeps");
    
    $.each(peepsByKey,function(key,value){
        drawPeep(value);
    });

    $(".peep").click(function(e) {
        
        showUserTweets(e.currentTarget.id);
    });
    setTimeout(function() {
        $.each(peepsByKey,function(key,value){
            var peepDiv = $("#" + value.handle);
            peepDiv.animate({
                left: getX(value.x),
                top: getY(value.y)
            },
            {
                duration: 500
            });
        });
    }, 1000);
};

var excludeTerm = function(){
    var groupName = $(this).text();
    $(this).hide("explode", 1500);
    excludedWords.push(groupName);
    socket.emit("update", {
        id: 'login',
        uri: 'SDC/Input/Users',
        action: 'modify',
        itemKey: username,
        item: {
            DeletedTerms: excludedWords.join(',')
        }
    });
};


var loadPeeps = function() {
    connectToApi(loadPeepsDom);
};